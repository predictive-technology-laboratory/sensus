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
using System.Linq;
using System.Collections.ObjectModel;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.Probes.Location
{
    public class ListeningPointsOfInterestProximityProbe : ListeningProbe, IPointsOfInterestProximityProbe
    {
        private ObservableCollection<PointOfInterestProximityTrigger> _triggers;
        private EventHandler<PositionEventArgs> _positionChangedHandler;

        public ObservableCollection<PointOfInterestProximityTrigger> Triggers
        {
            get { return _triggers; }
        }

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
                return "Points of Interest Proximity";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(PointOfInterestProximityDatum);
            }
        }

        public ListeningPointsOfInterestProximityProbe()
        {
            _triggers = new ObservableCollection<PointOfInterestProximityTrigger>();

            _positionChangedHandler = async (o, e) =>
            {
                List<Datum> data = new List<Datum>();

                SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose, GetType());

                if (e.Position != null)
                {
                    foreach (PointOfInterest pointOfInterest in SensusServiceHelper.Get().PointsOfInterest.Union(Protocol.PointsOfInterest))  // POIs are stored on the service helper (e.g., home locations) and the Protocol (e.g., bars), since the former are user-specific and the latter are universal.
                    {
                        double distanceToPointOfInterestMeters = pointOfInterest.KmDistanceTo(e.Position) * 1000;

                        foreach (PointOfInterestProximityTrigger trigger in _triggers)
                        {
                            if (pointOfInterest.Triggers(trigger, distanceToPointOfInterestMeters))
                            {
                                data.Add(new PointOfInterestProximityDatum(e.Position.Timestamp, pointOfInterest, distanceToPointOfInterestMeters, trigger));
                            }
                        }
                    }
                }

                if (data.Count > 0)
                {
                    foreach (Datum datum in data)
                    {
                        await StoreDatumAsync(datum);
                    }
                }
                else
                {
                    await StoreDatumAsync(null);
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
                string error = "Geolocation is not permitted on this device. Cannot start proximity probe.";
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
            throw new NotImplementedException();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }
    }
}
