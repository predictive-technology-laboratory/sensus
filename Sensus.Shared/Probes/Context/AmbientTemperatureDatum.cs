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
    public class AmbientTemperatureDatum : Datum
    {
        private double _degreesCelsius;

        [Anonymizable("Degrees (C)", new Type[] { typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty("Degrees (C)")]
        public double DegreesCelsius
        {
            get
            {
                return _degreesCelsius;
            }
            set
            {
                _degreesCelsius = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Degrees (C):  " + Math.Round(_degreesCelsius, 1);
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the temperature (C).
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return Math.Round(_degreesCelsius, 0);
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private AmbientTemperatureDatum() 
        {
        } 

        public AmbientTemperatureDatum(DateTimeOffset timestamp, double degreesCelcius)
            : base(timestamp)
        {
            _degreesCelsius = degreesCelcius;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Degrees (C):  " + _degreesCelsius;
        }
    }
}