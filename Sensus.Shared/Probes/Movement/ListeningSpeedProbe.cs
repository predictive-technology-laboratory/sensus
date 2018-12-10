//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Sensus.Probes.Location;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.Probes.Movement
{
    public class ListeningSpeedProbe : ListeningProbe
    {
        private EventHandler<PositionEventArgs> _positionChangedHandler;
        private Position _previousPosition;

        private readonly object _locker = new object();

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return true;
            }
        }

        [JsonIgnore]
        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting does not affect iOS. Android devices will use additional power to report all updates.";
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return "This setting does not affect iOS. Android devices will sleep and pause updates.";
            }
        }

        public sealed override string DisplayName
        {
            get
            {
                return "Speed";
            }
        }

        public sealed override Type DatumType
        {
            get
            {
                return typeof(SpeedDatum);
            }
        }

        public ListeningSpeedProbe()
        {
            _positionChangedHandler = async (o, e) =>
            {
                if (e.Position == null)
                {
                    return;
                }

                Datum datum = null;

                lock (_locker)
                {
                    SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose, GetType());

                    if (_previousPosition == null)
                    {
                        _previousPosition = e.Position;
                    }
                    else if (e.Position.Timestamp > _previousPosition.Timestamp)  // it has happened (rarely) that positions come in out of order...drop any such positions.
                    {
                        datum = new SpeedDatum(e.Position.Timestamp, _previousPosition, e.Position);
                        _previousPosition = e.Position;
                    }
                }

                if (datum != null)
                {
                    await StoreDatumAsync(datum);
                }
            };
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start speed probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected sealed override async Task StartListeningAsync()
        {
            _previousPosition = null;
            await GpsReceiver.Get().AddListenerAsync(_positionChangedHandler, false);
        }

        public override async Task ResetAsync()
        {
            await base.ResetAsync();

            _previousPosition = null;
        }

        protected sealed override async Task StopListeningAsync()
        {
            await GpsReceiver.Get().RemoveListenerAsync(_positionChangedHandler);
            _previousPosition = null;
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as SpeedDatum).KPH);
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return new DateTimeAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Time"
                }
            };
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Speed (KPH)"
                }
            };
        }
    }
}
