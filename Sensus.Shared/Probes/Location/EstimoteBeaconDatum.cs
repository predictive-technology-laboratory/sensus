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

#if __ANDROID__
using Region = EstimoteSdk.Observation.Region.Beacon.BeaconRegion;
#elif __IOS__
using Region = CoreLocation.CLBeaconRegion;
#endif

namespace Sensus.Probes.Location
{
    public class EstimoteBeaconDatum : Datum
    {
        [StringProbeTriggerProperty]
        public string Identifier { get; set; }

        [StringProbeTriggerProperty]
        public string UUID { get; set; }

        [DoubleProbeTriggerProperty]
        public int? Major { get; set; }

        [DoubleProbeTriggerProperty]
        public int? Minor { get; set; }

        [BooleanProbeTriggerProperty]
        public bool Entering { get; set; }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get
            {
                return Identifier + ":" + UUID + ":" + Major + ":" + Minor + ":" + (Entering ? "In Range" : "Out of Range");
            }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private EstimoteBeaconDatum()
        {
        }

        public EstimoteBeaconDatum(DateTimeOffset timestamp, Region region, bool entering)
            : base(timestamp)
        {
            Identifier = region.Identifier;
            Entering = entering;

#if __ANDROID__
            UUID = region.ProximityUUID.ToString();
            Major = region.Major;
            Minor = region.Minor;
#elif __IOS__
            UUID = region.ProximityUuid.ToString();
            Major = region.Major.Int32Value;
            Minor = region.Minor.Int32Value;
#endif
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Identifier:  " + Identifier + Environment.NewLine +
                   "UUID:  " + UUID + Environment.NewLine +
                   "Major:  " + Major + Environment.NewLine +
                   "Minor:  " + Minor + Environment.NewLine +
                   "Entering:  " + Entering;
        }
    }
}