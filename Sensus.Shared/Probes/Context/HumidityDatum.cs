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

namespace Sensus.Probes.Context
{
    public class HumidityDatum : Datum
    {
        private double _relativeHumidity;        

        [Anonymizable("Relative Humidity", new Type[] { typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty("Relative Humidity")]
        public double RelativeHumidity
        {
            get
            {
                return _relativeHumidity;
            }
            set
            {
                _relativeHumidity = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Relative Humidity:  " + Math.Round(_relativeHumidity, 1);
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the relative Humidity.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return Math.Round(_relativeHumidity, 0);
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private HumidityDatum() 
        {
        } 

        public HumidityDatum(DateTimeOffset timestamp, double relativeHumidity)
            : base(timestamp)
        {
            _relativeHumidity = relativeHumidity;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Relative Humidity:  " + _relativeHumidity;
        }
    }
}