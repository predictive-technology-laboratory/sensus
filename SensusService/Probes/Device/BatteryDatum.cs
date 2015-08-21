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
using SensusService.Probes.User.Scripts.ProbeTriggerProperties;
using System;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Probes.Device
{
    public class BatteryDatum : Datum
    {
        private double _level;

        [NumberProbeTriggerProperty]
        [Anonymizable(null, typeof(DoubleRoundingTensAnonymizer), false)]
        public double Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public override string DisplayDetail
        {
            get { return "Level:  " + Math.Round(_level, 2); }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private BatteryDatum() { }

        public BatteryDatum(DateTimeOffset timestamp, double level)
            : base(timestamp)
        {
            _level = level;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Level:  " + _level;
        }
    }
}
