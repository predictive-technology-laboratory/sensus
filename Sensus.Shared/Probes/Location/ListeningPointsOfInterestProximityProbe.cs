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

        /// <summary>
        /// This <see cref="Probe"/> uses continuous GPS listening and will have a significant negative impact on battery life.
        /// </summary>
        protected override bool WillHaveSignificantNegativeImpactOnBattery => true;

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

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            await GpsReceiver.Get().AddListenerAsync(_positionChangedHandler, false);
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();

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