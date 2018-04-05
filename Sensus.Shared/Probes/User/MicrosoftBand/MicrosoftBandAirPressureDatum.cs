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
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.User.MicrosoftBand
{
    public class MicrosoftBandAirPressureDatum : Datum
    {
        private double _airPressure;

        [Anonymizable("Air Pressure:", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double AirPressure
        {
            get
            {
                return _airPressure;
            }

            set
            {
                _airPressure = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Air Pressure:  " + Math.Round(_airPressure, 0);
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the air pressue.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _airPressure;
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandAirPressureDatum()
        {
        }

        public MicrosoftBandAirPressureDatum(DateTimeOffset timestamp, double airPressure)
            : base(timestamp)
        {
            _airPressure = airPressure;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Air Pressure:  " + _airPressure;
        }
    }
}