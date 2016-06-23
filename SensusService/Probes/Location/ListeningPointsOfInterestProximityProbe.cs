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

namespace SensusService.Probes.Location
{
    public class ListeningPointsOfInterestProximityProbe : ListeningProbe, IPointsOfInterestProximityProbe
    {
        private ObservableCollection<PointOfInterestProximityTrigger> _triggers;
        private EventHandler<PositionEventArgs> _positionChangedHandler;

        private readonly object _locker = new object();

        public ObservableCollection<PointOfInterestProximityTrigger> Triggers
        {
            get { return _triggers; }
        }

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        [JsonIgnore]
        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting does not affect iOS or Android.";
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return "This setting does not affect iOS or Android.";
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

            _positionChangedHandler = (o, e) =>
            {
                lock (_locker)
                {
                    SensusServiceHelper.Get().Logger.Log("Received position change notification.", LoggingLevel.Verbose, GetType());

                    bool datumStored = false;

                    if (e.Position != null)
                        foreach (PointOfInterest pointOfInterest in SensusServiceHelper.Get().PointsOfInterest.Union(Protocol.PointsOfInterest))  // POIs are stored on the service helper (e.g., home locations) and the Protocol (e.g., bars), since the former are user-specific and the latter are universal.
                        {
                            double distanceToPointOfInterestMeters = pointOfInterest.KmDistanceTo(e.Position) * 1000;

                            foreach (PointOfInterestProximityTrigger trigger in _triggers)
                                if (pointOfInterest.Triggers(trigger, distanceToPointOfInterestMeters))
                                {
                                    StoreDatum(new PointOfInterestProximityDatum(e.Position.Timestamp, pointOfInterest, distanceToPointOfInterestMeters, trigger));
                                    datumStored = true;
                                }
                        }

                    if (!datumStored)
                        StoreDatum(null);
                }
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start proximity probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected sealed override void StartListening()
        {
            GpsReceiver.Get().AddListener(_positionChangedHandler, false);
        }

        protected sealed override void StopListening()
        {
            GpsReceiver.Get().RemoveListener(_positionChangedHandler);
        }
    }
}