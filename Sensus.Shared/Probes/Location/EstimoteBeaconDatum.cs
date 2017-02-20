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
using EstimoteSdk;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Location
{
    public class EstimoteBeaconDatum : Datum
    {
        [StringProbeTriggerProperty]
        public string Name { get; set; }

        [StringProbeTriggerProperty]
        public string UUID { get; set; }

        public int? Major { get; set; }

        public int? Minor { get; set; }

        public override string DisplayDetail
        {
            get
            {
                return UUID + ":" + Major + ":" + Minor;
            }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private EstimoteBeaconDatum()
        {
        }

        public EstimoteBeaconDatum(DateTimeOffset timestamp, Region region)
            : base(timestamp)
        {
            Name = region.Identifier;
            UUID = region.ProximityUUID.ToString();
            Major = region.Major;
            Minor = region.Minor;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Name:  " + Name + Environment.NewLine +
                   "UUID:  " + UUID + Environment.NewLine +
                   "Major:  " + Major + Environment.NewLine +
                   "Minor:  " + Minor;
        }
    }
}