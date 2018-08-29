// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Newtonsoft.Json;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Listens continuously for location changes.
    /// </summary>
    public class ListeningLocationProbe : ListeningProbe
    {
        private EventHandler<PositionEventArgs> _positionChangedHandler;

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
            get { return "GPS Location"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(LocationDatum); }
        }

        public ListeningLocationProbe()
        {
            _positionChangedHandler = async (o, e) =>
            {
                SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose, GetType());

                await StoreDatumAsync(new LocationDatum(e.Position.Timestamp, e.Position.Accuracy, e.Position.Latitude, e.Position.Longitude));
            };
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start location probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected sealed override async Task StartListeningAsync()
        {
            await GpsReceiver.Get().AddListenerAsync(_positionChangedHandler, false);
        }

        protected sealed override async Task StopListeningAsync()
        {
            await GpsReceiver.Get().RemoveListenerAsync(_positionChangedHandler);
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            LocationDatum location = datum as LocationDatum;
            return new ChartDataPoint(location.Longitude, location.Latitude);
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Longitude"
                }
            };
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Latitude"
                }
            };
        }
    }
}

