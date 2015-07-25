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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;
using SensusService.Probes.User.ProbeTriggerProperties;
using Xamarin.Geolocation;

namespace SensusService.Probes.Movement
{
    public class SpeedDatum : ImpreciseDatum
    {
        #region static members
        /// <summary>
        /// Calculates distance (KM) given two lat-lon positions (http://www.movable-type.co.uk/scripts/latlong.html)
        /// </summary>
        /// <returns>Distance in KM.</returns>
        /// <param name="previousPosition">Previous position.</param>
        /// <param name="currentPosition">Current position.</param>
        public static double CalculateDistanceKM(Position previousPosition, Position currentPosition)
        {
            double φ1 = DegreesToRadians(previousPosition.Latitude);
            double φ2 = DegreesToRadians(currentPosition.Latitude);
            double Δφ = DegreesToRadians(currentPosition.Latitude - previousPosition.Latitude);
            double Δλ = DegreesToRadians(currentPosition.Longitude - previousPosition.Longitude);

            double a = Math.Pow(Math.Sin(Δφ / 2), 2) +
                Math.Cos(φ1) *
                Math.Cos(φ2) *
                Math.Pow(Math.Sin(Δλ / 2), 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return 6371 * c;
        }
            
        public static double CalculateSpeedKPH(Position previousPosition, Position currentPosition, out double distanceKM, out double timeHours)
        {            
            distanceKM = CalculateDistanceKM(previousPosition, currentPosition);
            timeHours = new TimeSpan(currentPosition.Timestamp.Ticks - previousPosition.Timestamp.Ticks).TotalHours;

            return distanceKM / timeHours;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians / Math.PI * 180;
        }
        #endregion

        private double _kph;

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTensAnonymizer), typeof(DoubleRoundingHundredsAnonymizer) }, -1)]
        public double KPH
        {
            get { return _kph; }
            set { _kph = value; }
        }

        public override string DisplayDetail
        {
            get { return Math.Round(_kph, 1) + " (+/- " + Math.Round(Accuracy, 1) + ")" + " KPH"; }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private SpeedDatum() { }

        public SpeedDatum(DateTimeOffset timestamp, Position previousPosition, Position currentPosition)
            : base(timestamp, 0)
        {
            double distanceKM;
            double timeHours;
            _kph = SpeedDatum.CalculateSpeedKPH(previousPosition, currentPosition, out distanceKM, out timeHours);

            if (previousPosition.Accuracy >= 0 && currentPosition.Accuracy >= 0)
            {
                double maximumDistanceKM = distanceKM + previousPosition.Accuracy / 1000f + currentPosition.Accuracy / 1000f;
                double maximumSpeedKPH = maximumDistanceKM / timeHours;
                Accuracy = (float)(maximumSpeedKPH - _kph);
            }
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "KPH:  " + _kph;
        }
    }
}
