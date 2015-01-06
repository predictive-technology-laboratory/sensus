#region copyright
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
#endregion

using SensusService.Probes.Location;
using System;
using System.Collections.Generic;
using System.Device.Location;

namespace SensusService.Probes.Movement
{
    public class PollingSpeedProbe : PollingProbe
    {
        private PollingLocationProbe _locationProbe;
        private LocationDatum _previousLocation;

        protected sealed override string DefaultDisplayName
        {
            get { return "Speed"; }
        }

        public sealed override int PollingSleepDurationMS
        {
            get { return base.PollingSleepDurationMS; }
            set { base.PollingSleepDurationMS = _locationProbe.PollingSleepDurationMS = value; }
        }

        public sealed override int DefaultPollingSleepDurationMS
        {
            get { return 5000; }
        }

        public PollingSpeedProbe()
        {
            _locationProbe = new PollingLocationProbe();

            _locationProbe.PollingSleepDurationMS = DefaultPollingSleepDurationMS;
        }

        public override void Start()
        {
            lock (this)
            {
                _locationProbe.Start();

                base.Start();
            }
        }

        protected override IEnumerable<Datum> Poll()
        {
            lock (this)
            {
                LocationDatum currentLocation = _locationProbe.MostRecentlyStoredDatum as LocationDatum;

                Datum[] data = null;

                if (_previousLocation == null || currentLocation == null | currentLocation.Timestamp == _previousLocation.Timestamp)
                    data = new Datum[] { };
                else
                {
                    GeoCoordinate previousCoordinate = new GeoCoordinate(_previousLocation.Latitude, _previousLocation.Longitude);
                    GeoCoordinate currentCoordinate = new GeoCoordinate(currentLocation.Latitude, currentLocation.Longitude);

                    double reportedDistKM = previousCoordinate.GetDistanceTo(currentCoordinate);
                    double minDistKM = reportedDistKM - _previousLocation.Accuracy / 1000f - currentLocation.Accuracy / 1000f;
                    double maxDistKM = reportedDistKM + _previousLocation.Accuracy / 1000f + currentLocation.Accuracy / 1000f;

                    double elapsedHours = new TimeSpan(currentLocation.Timestamp.Ticks - _previousLocation.Timestamp.Ticks).TotalHours;

                    double reportedSpeedKPH = reportedDistKM / elapsedHours;
                    double minSpeedKPH = minDistKM / elapsedHours;
                    double maxSpeedKPH = maxDistKM / elapsedHours;

                    data = new SpeedDatum[] { new SpeedDatum(this, currentLocation.Timestamp, (float)(maxSpeedKPH - minSpeedKPH) / 2f, (float)reportedSpeedKPH) };
                }

                if (currentLocation != null)
                    _previousLocation = currentLocation;

                return data;
            }
        }

        public override void Stop()
        {
            lock (this)
            {
                _locationProbe.Stop();

                base.Stop();
            }
        }
    }
}
