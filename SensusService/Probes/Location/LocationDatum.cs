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

using Newtonsoft.Json;
using SensusService.Probes.User.Scripts.ProbeTriggerProperties;
using System;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Probes.Location
{
    public class LocationDatum : ImpreciseDatum
    {
        private double _latitude;
        private double _longitude;

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, 1)]  // rounding to hundredths is roughly 1km
        public double Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, 1)]  // rounding to hundredths is roughly 1km
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
