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

namespace Sensus.Probes.User.Health
{
    public class NumberOfTimesFallenDatum : Datum
    {
        private int _numberOfTimesFallen;

        [DoubleProbeTriggerProperty("Number of Times Fallen")]
        [Anonymizable("Number of Times Fallen:", typeof(DoubleRoundingTensAnonymizer), false)]
        public int NumberOfTimesFallen
        {
            get
            {
                return _numberOfTimesFallen;
            }
            set
            {
                _numberOfTimesFallen = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Number of Times Fallen:  " + _numberOfTimesFallen;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the Number of Times Fallen.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _numberOfTimesFallen;
            }
        }

        public NumberOfTimesFallenDatum(DateTimeOffset timestamp, int numberOfTimesFallen)
            : base(timestamp)
        {
            _numberOfTimesFallen = numberOfTimesFallen;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Number of Times Fallen:  " + _numberOfTimesFallen;
        }
    }
}