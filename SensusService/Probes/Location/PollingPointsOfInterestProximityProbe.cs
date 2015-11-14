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
using SensusService.Probes;
using System.Collections.Generic;
using SensusUI.UiProperties;
using Xamarin.Geolocation;
using SensusService.Probes.Location;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;

namespace SensusService.Probes.Location
{
    public class PollingPointsOfInterestProximityProbe : PollingProbe, IPointsOfInterestProximityProbe
    {
        private ObservableCollection<PointOfInterestProximityTrigger> _triggers;

        public ObservableCollection<PointOfInterestProximityTrigger> Triggers
        {
            get { return _triggers; }
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

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return 30000; // every 30 seconds
            }
        }

        public PollingPointsOfInterestProximityProbe()
        {
            _triggers = new ObservableCollection<PointOfInterestProximityTrigger>();
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            Position currentPosition = GpsReceiver.Get().GetReading(cancellationToken);

            if (currentPosition == null)
                throw new Exception("Failed to get GPS reading.");
            else
                foreach (PointOfInterest pointOfInterest in SensusServiceHelper.Get().PointsOfInterest.Union(Protocol.PointsOfInterest))  // POIs are stored on the service helper (e.g., home locations) and the Protocol (e.g., bars), since the former are user-specific and the latter are universal.
                {
                    double distanceToPointOfInterestMeters = pointOfInterest.KmDistanceTo(currentPosition) * 1000;

                    foreach (PointOfInterestProximityTrigger trigger in _triggers)
                        if (pointOfInterest.Triggers(trigger, distanceToPointOfInterestMeters))
                            data.Add(new PointOfInterestProximityDatum(currentPosition.Timestamp, pointOfInterest, distanceToPointOfInterestMeters, trigger));
                }

            if (data.Count == 0)
                data.Add(null);

            return data;
        }
    }
}