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
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using Newtonsoft.Json;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;

namespace Sensus.Probes.Location
{
    public class EstimoteBeaconDatum : Datum
    {
        [StringProbeTriggerProperty]
        [Anonymizable("Beacon Identifier:", typeof(StringHashAnonymizer), false)]
        public string BeaconIdentifier { get; set; }

        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Value { get; set; }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get
            {
                return BeaconIdentifier + ":  " + Value;
            }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private EstimoteBeaconDatum()
        {
        }

        public EstimoteBeaconDatum(DateTimeOffset timestamp, string beaconIdentifier, string value)
            : base(timestamp)
        {
            BeaconIdentifier = beaconIdentifier;
            Value = value;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Beacon Identifier:  " + BeaconIdentifier + Environment.NewLine +
                   "Value:  " + Value;
        }
    }
}