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

namespace SensusService.Probes.Movement
{
    public class PollingSpeedProbe : PollingProbe
    {
        private PollingLocationProbe _locationProbe;
        private LocationDatum _previousLocation;

        protected sealed override string DefaultDisplayName
        {
            get { return "Speed (Polling)"; }
        }

        public sealed override int PollingSleepDurationMS
        {
            get { return base.PollingSleepDurationMS; }
            set { base.PollingSleepDurationMS = _locationProbe.PollingSleepDurationMS = value; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(SpeedDatum); }
        }

        public sealed override int DefaultPollingSleepDurationMS
        {
            get { return 5000; }
        }

        public PollingSpeedProbe()
        {
            _locationProbe = new PollingLocationProbe();
            _locationProbe.StoreData = false;
            _locationProbe.PollingSleepDurationMS = DefaultPollingSleepDurationMS;
        }

        public override void Start()
        {
            lock (this)
            {
                _previousLocation = null;  // do this before starting the base-class poller so it doesn't race to grab a stale previous location.
                base.Start();               
                _locationProbe.Start();
            }
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private double RadiansToDegrees(double radians)
        {
            return radians / Math.PI * 180;
        }

        protected override IEnumerable<Datum> Poll()
        {
            lock (this)
            {
                LocationDatum currentLocation = _locationProbe.MostRecentDatum as LocationDatum;

                Datum[] data = null;

                if (_previousLocation == null || currentLocation == null || currentLocation.Timestamp == _previousLocation.Timestamp)
                    data = new Datum[] { };
                else
                {
                    // http://www.movable-type.co.uk/scripts/latlong.html

                    double φ1 = DegreesToRadians(_previousLocation.Latitude);
                    double φ2 = DegreesToRadians(currentLocation.Latitude);
                    double Δφ = DegreesToRadians(currentLocation.Latitude - _previousLocation.Latitude);
                    double Δλ = DegreesToRadians(currentLocation.Longitude - _previousLocation.Longitude);

                    double a = Math.Pow(Math.Sin(Δφ / 2), 2) +
                               Math.Cos(φ1) *
                               Math.Cos(φ2) *
                               Math.Pow(Math.Sin(Δλ / 2), 2);

                    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                    var reportedDistKM = 6371 * c;

                    double elapsedHours = new TimeSpan(currentLocation.Timestamp.Ticks - _previousLocation.Timestamp.Ticks).TotalHours;
                    double reportedSpeedKPH = reportedDistKM / elapsedHours;

                    float accuracy = 0;
                    if (_previousLocation.Accuracy >= 0 && currentLocation.Accuracy >= 0)
                    {
                        double maxDistKM = reportedDistKM + _previousLocation.Accuracy / 1000f + currentLocation.Accuracy / 1000f;
                        double maxSpeedKPH = maxDistKM / elapsedHours;
                        accuracy = (float)(maxSpeedKPH - reportedSpeedKPH);
                    }

                    data = new SpeedDatum[] { new SpeedDatum(this, currentLocation.Timestamp, accuracy, (float)reportedSpeedKPH) };
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
                base.Stop();
                _locationProbe.Stop();
                _previousLocation = null;  // reset previous location so it doesn't get used when this probe is restarted.
            }
        }
    }
}
