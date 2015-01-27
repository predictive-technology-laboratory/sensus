#region copyright
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
#endregion

using Newtonsoft.Json;
using SensusService.Probes.User.ProbeTriggerProperties;
using System;

namespace SensusService.Probes.Network
{
    public class WlanDatum : Datum
    {
        private string _accessPointBSSID;

        [TextProbeTriggerProperty]
        public string AccessPointBSSID
        {
            get { return _accessPointBSSID; }
            set { _accessPointBSSID = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return "AP BSSID:  " + _accessPointBSSID; }
        }

        public WlanDatum(Probe probe, DateTimeOffset timestamp, string accessPointBSSID)
            : base(probe, timestamp)
        {
            _accessPointBSSID = accessPointBSSID;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "AP BSSID:  " + _accessPointBSSID;
        }
    }
}
