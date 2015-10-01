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
    public class ActiveEnergyDatum : Datum
    {
        private double _enrgycalories;

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double EnergyCalories
        {
            get
            {
                return _enrgycalories;
            }
            set
            {
                _enrgycalories = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Active Energy (Calories):  " + Math.Round(_enrgycalories, 1);
            }
        }

        public ActiveEnergyDatum(DateTimeOffset timestamp, double enrgycalories)
            : base(timestamp)
        {
            _enrgycalories = enrgycalories;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                "Active Energy (Calories):  " + _enrgycalories;
        }
    }
}
