﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

namespace Sensus.Probes.User.Health
{
    public class StepCountDatum : Datum
    {
        private double _stepCount;

        [DoubleProbeTriggerProperty("Step Count")]
        [Anonymizable("Step Count:", typeof(DoubleRoundingTensAnonymizer), false)]
        public double StepCount
        {
            get
            {
                return _stepCount;
            }
            set
            {
                _stepCount = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Step Count:  " + _stepCount;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the step count.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _stepCount;
            }
        }

        public StepCountDatum(DateTimeOffset timestamp, double stepCount)
            : base(timestamp)
        {
            _stepCount = stepCount;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Step Count:  " + _stepCount;
        }
    }
}