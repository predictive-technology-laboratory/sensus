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
        public const double EARTH_RADIUS_MILES = 3956;
        public const double EARTH_RADIUS_KILOMETERS = 6367;

        public static double ToRadians(double value)
        {
            return value * (Math.PI / 180);
        }

        public static double DiffRadians(double value1, double value2)
        {
            return ToRadians(value2) - ToRadians(value1);
        }

        public static double CalculateDistanceMiles(double lat1, double lon1, double lat2, double lon2)
        {
            return CalculateDistance(lat1, lon1, lat2, lon2, EARTH_RADIUS_MILES);
        }

        public static double CalculateDistanceKilometers(double lat1, double lon1, double lat2, double lon2)
        {
            return CalculateDistance(lat1, lon1, lat2, lon2, EARTH_RADIUS_KILOMETERS);
        }

        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2, double radius)
        {
            return radius * 2 * Math.Asin(Math.Min(1, Math.Sqrt((Math.Pow(Math.Sin((DiffRadians(lat1, lat2)) / 2.0), 2.0) +
                                            Math.Cos(ToRadians(lat1)) *
                                            Math.Cos(ToRadians(lat2)) *
                                            Math.Pow(Math.Sin((DiffRadians(lon1, lon2)) / 2.0), 2.0)))));
        }

        private PollingLocationProbe _locationProbe;
        private LocationDatum _previousLocation;

        protected sealed override string DefaultDisplayName
        {
            get { return "Speed"; }
        }

        public sealed override int DefaultPollingSleepDurationMS
        {
            get { return 5000; }
        }

        public PollingSpeedProbe()
        {
            _locationProbe = new PollingLocationProbe();
        }

        public override void Start()
        {
            _locationProbe.Start();

            base.Start();
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
                    double reportedKilometers = CalculateDistanceKilometers(_previousLocation.Latitude, _previousLocation.Longitude, currentLocation.Latitude, currentLocation.Longitude);
                    double minKilometers = reportedKilometers - _previousLocation.Accuracy / 1000f - currentLocation.Accuracy / 1000f;
                    double maxKilometers = reportedKilometers + _previousLocation.Accuracy / 1000f + currentLocation.Accuracy / 1000f;

                    double elapsedHours = new TimeSpan(currentLocation.Timestamp.Ticks - _previousLocation.Timestamp.Ticks).TotalHours;

                    double reportedSpeed = reportedKilometers / elapsedHours;
                    double minSpeed = minKilometers / elapsedHours;
                    double maxSpeed = maxKilometers / elapsedHours;

                    data = new SpeedDatum[] { new SpeedDatum(this, currentLocation.Timestamp, (float)(maxSpeed - minSpeed) / 2f, (float)reportedSpeed) };
                }

                if (currentLocation != null)
                    _previousLocation = currentLocation;

                return data;
            }
        }

        public override void Stop()
        {
            _locationProbe.Stop();

            base.Stop();
        }
    }
}
