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
    public class LocationDatum : ImpreciseDatum, ILocationDatum
    {
        private double _latitude;
        private double _longitude;

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

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Latitude:  " + _latitude + Environment.NewLine +
                   "Longitude:  " + _longitude;
        }
    }
}
