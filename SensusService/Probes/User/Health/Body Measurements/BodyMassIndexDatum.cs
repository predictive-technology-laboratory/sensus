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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;
using SensusService.Probes.User.ProbeTriggerProperties;

namespace SensusService.Probes.User.Health
{
    public class BodyMassIndexDatum : Datum
    {
        private double _BMI;

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double BodyMassIndex
        {
            get
            {
                return _BMI;
            }
            set
            {
                _BMI = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Body Mass Index (BMI):  " + Math.Round(_BMI, 1);
            }
        }

        public BodyMassIndexDatum(DateTimeOffset timestamp, double BMI)
            : base(timestamp)
        {
            _BMI = BMI;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                "Body Mass Index (BMI):  " + _BMI;
        }
    }
}