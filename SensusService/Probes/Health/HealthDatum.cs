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
using SensusService.Probes.User.ProbeTriggerProperties;
using System;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Probes.Health
{
    public class HealthDatum : Datum
    {

        private double _height;
        private double _age;
        private double _weight;

        [NumberProbeTriggerProperty]
        [Anonymizable(null, typeof(DoubleRoundingTensAnonymizer), false)]
        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

        [NumberProbeTriggerProperty]
        [Anonymizable(null, typeof(DoubleRoundingTensAnonymizer), false)]
        public double Age
        {
            get { return _age; }
            set { _age = value; }
        }

        [NumberProbeTriggerProperty]
        [Anonymizable(null, typeof(DoubleRoundingTensAnonymizer), false)]
        public double Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }

        public override string DisplayDetail
        {
            get { return Math.Round(_height, 4) + " (Height), " + Math.Round(_weight, 4) + " (Weight), " + Math.Round(_age, 2) + " (Age)"; }
        }



        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private HealthDatum() { }

        public HealthDatum(DateTimeOffset timestamp, double height, double age, double weight)
            : base(timestamp)
        {
            _height = height;
            _age = age;
            _weight = weight;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                "Height:  " + _height + Environment.NewLine +
                "Age:  " + _age + Environment.NewLine +
                "Weight:  " + _weight;
        }
    }
}
