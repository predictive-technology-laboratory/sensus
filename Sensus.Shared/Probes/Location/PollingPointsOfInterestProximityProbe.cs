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
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Plugin.Geolocator.Abstractions;
using Syncfusion.SfChart.XForms;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;

namespace Sensus.Probes.Location
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

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start POI probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override async Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            Position currentPosition = await GpsReceiver.Get().GetReadingAsync(cancellationToken, false);

            if (currentPosition == null)
            {
                throw new Exception("Failed to get GPS reading.");
            }
            else
            {
                foreach (PointOfInterest pointOfInterest in SensusServiceHelper.Get().PointsOfInterest.Union(Protocol.PointsOfInterest))  // POIs are stored on the service helper (e.g., home locations) and the Protocol (e.g., bars), since the former are user-specific and the latter are universal.
                {
                    double distanceToPointOfInterestMeters = pointOfInterest.KmDistanceTo(currentPosition) * 1000;

                    foreach (PointOfInterestProximityTrigger trigger in _triggers)
                    {
                        if (pointOfInterest.Triggers(trigger, distanceToPointOfInterestMeters))
                        {
                            data.Add(new PointOfInterestProximityDatum(currentPosition.Timestamp, pointOfInterest, distanceToPointOfInterestMeters, trigger));
                        }
                    }
                }
            }

            // let the system know that we polled but didn't get any data
            if (data.Count == 0)
            {
                data.Add(null);
            }

            return data;
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