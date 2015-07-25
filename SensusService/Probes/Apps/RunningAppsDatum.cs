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

namespace SensusService.Probes.Apps
{
    public class RunningAppsDatum : Datum
    {
        private string _name;
        private string _description;

        [TextProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [TextProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public override string DisplayDetail
        {
            get { return "Name:  " + _name + " (" + _description + ")"; }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private RunningAppsDatum() { }

        public RunningAppsDatum(DateTimeOffset timestamp, string name, string description)
            : base(timestamp)
        {
            _name = name ?? "";
            _description = description ?? "";
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Name:  " + _name + Environment.NewLine +
                   "Description:  " + _description;
        }
    }
}
