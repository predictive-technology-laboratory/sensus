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
using Plugin.Geolocator.Abstractions;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// 
    /// Location in degrees latitude and longitude. Can be anonymized by rounding significant digits from these degree values. For example, 
    /// rounding the latitude/longitude location 38.64/-78.35 to tenths produces 38.6/-78.4, reducing spatial fidelity by approximately 4 miles. 
    /// Similarly, rounding to hundredths will reduce spatial fidelity by approximately 0.4 miles, and rounding to thousandths will reduce 
    /// spatial fidelity by approximately 0.04 miles (200 feet).
    /// 
    /// </summary>
    public class LocationDatum : ImpreciseDatum
    {
        private double _latitude;
        private double _longitude;
        private double? _distanceTraveled;


        /// <summary>
        /// Latitude coordinate, measured in decimal degrees north and south of the equator per the WGS 1984 datum. 
        /// </summary>
        /// <value>The latitude.</value>
        [DoubleProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
        public double Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        /// <summary>
        /// Longitude coordinate, measured in decimal degrees west and east of the prime meridian per the WGS 1984 datum.
        /// </summary>
        /// <value>The longitude.</value>
        [DoubleProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(LongitudeParticipantOffsetGpsAnonymizer), typeof(LongitudeStudyOffsetGpsAnonymizer), typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
        public double Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }


        /// <summary>
        /// Distance Traveled in kilometers between the last location reading and this one. 
        /// </summary>
        /// <value>Distance traveled between the last location reading at this one.</value>
        [DoubleProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
        public double? DistanceTraveled
        {
            get { return _distanceTraveled; }
            set { _distanceTraveled = value; }
        }

        public override string DisplayDetail
        {
            get { return Math.Round(_latitude, 2) + " (lat), " + Math.Round(_longitude, 2) + " (lon)"; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the [lat,lon] location.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return "[" + Math.Round(_latitude, 2) + "," + Math.Round(_longitude, 2) + "]";
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private LocationDatum() { }

        public LocationDatum(DateTimeOffset timestamp, double accuracy, double latitude, double longitude)
            : base(timestamp, accuracy)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        public LocationDatum(DateTimeOffset timestamp, double accuracy, double latitude, double longitude, double? lastLatitude, double? lastLongitude)
                : base(timestamp, accuracy)
        {
            _latitude = latitude;
            _longitude = longitude;
            //TODO:  We might want to refactor this so it isn't calling the into the speed datum
            if (lastLatitude.HasValue && lastLongitude.HasValue)
            {
                _distanceTraveled = Movement.SpeedDatum.CalculateDistanceKM(new Position(lastLatitude.Value, lastLongitude.Value),
                                                                            new Position(latitude, longitude));
            }
       }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Latitude:  " + _latitude + Environment.NewLine +
                   "Longitude:  " + _longitude;
        }
    }
}
