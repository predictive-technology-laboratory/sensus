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

using SensusService.Probes.Location;
using System;
using System.Collections.Generic;
using Xamarin.Geolocation;

namespace SensusService.Probes.Movement
{
    public class PollingSpeedProbe : PollingProbe
    {
        private Position _previousPosition;

        private readonly object _locker = new object();

        protected sealed override string DefaultDisplayName
        {
            get { return "Speed"; }
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
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!GpsReceiver.Get().Locator.IsGeolocationEnabled)
            {
                string error = "Geolocation is not enabled on this device. Cannot start speed probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        public override void Start()
        {
            lock (_locker)
            {
                _previousPosition = null;  // do this before starting the base-class poller so it doesn't race to grab a stale previous location.
                base.Start();               
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
            lock (_locker)
            {
                Position currentPosition = GpsReceiver.Get().GetReading();

                Datum[] data = null;

                if (_previousPosition == null || currentPosition == null || currentPosition.Timestamp == _previousPosition.Timestamp)
                    data = new Datum[] { };
                else
                {
                    // http://www.movable-type.co.uk/scripts/latlong.html

                    double φ1 = DegreesToRadians(_previousPosition.Latitude);
                    double φ2 = DegreesToRadians(currentPosition.Latitude);
                    double Δφ = DegreesToRadians(currentPosition.Latitude - _previousPosition.Latitude);
                    double Δλ = DegreesToRadians(currentPosition.Longitude - _previousPosition.Longitude);

                    double a = Math.Pow(Math.Sin(Δφ / 2), 2) +
                               Math.Cos(φ1) *
                               Math.Cos(φ2) *
                               Math.Pow(Math.Sin(Δλ / 2), 2);

                    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                    var reportedDistKM = 6371 * c;

                    double elapsedHours = new TimeSpan(currentPosition.Timestamp.Ticks - _previousPosition.Timestamp.Ticks).TotalHours;
                    double reportedSpeedKPH = reportedDistKM / elapsedHours;

                    float accuracy = 0;
                    if (_previousPosition.Accuracy >= 0 && currentPosition.Accuracy >= 0)
                    {
                        double maxDistKM = reportedDistKM + _previousPosition.Accuracy / 1000f + currentPosition.Accuracy / 1000f;
                        double maxSpeedKPH = maxDistKM / elapsedHours;
                        accuracy = (float)(maxSpeedKPH - reportedSpeedKPH);
                    }

                    data = new SpeedDatum[] { new SpeedDatum(currentPosition.Timestamp, accuracy, (float)reportedSpeedKPH) };
                }

                if (currentPosition != null)
                    _previousPosition = currentPosition;

                return data;
            }
        }

        public override void Stop()
        {
            lock (_locker)
            {
                base.Stop();
                _previousPosition = null;  // reset previous location so it doesn't get used when this probe is restarted.
            }
        }
    }
}