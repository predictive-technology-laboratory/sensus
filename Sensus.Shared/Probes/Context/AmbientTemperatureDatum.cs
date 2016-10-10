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
using SensusService.Probes.User.Scripts.ProbeTriggerProperties;
using Newtonsoft.Json;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService
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