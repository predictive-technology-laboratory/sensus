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
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using Plugin.Geolocator.Abstractions;

namespace Sensus.Probes.Movement
{
    public class SpeedDatum : ImpreciseDatum, ISpeedDatum
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

        [DoubleProbeTriggerProperty]
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
        /// Gets the string placeholder value, which is the speed (KPH).
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return Math.Round(_kph, 1);
            }
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
