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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;
using SensusService.Probes.User.ProbeTriggerProperties;

namespace SensusService.Probes.User.Health
{
    public class HeightDatum : Datum
    {
        private double _heightInches;

        [NumberProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double HeightInches
        {
            get
            {
                return _heightInches;
            }
            set
            {
                _heightInches = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Height (Inches):  " + Math.Round(_heightInches, 1);
            }
        }

        public HeightDatum(DateTimeOffset timestamp, double heightInches)
            : base(timestamp)
        {
            _heightInches = heightInches;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                "Height (Inches):  " + _heightInches;
        }
    }
}