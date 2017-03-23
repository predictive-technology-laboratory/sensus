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

namespace Sensus.Probes.Network
{
    public class WlanDatum : Datum
    {
        private string _accessPointBSSID;

        [StringProbeTriggerProperty("Wireless Access Point")]
        [Anonymizable("Wireless Access Point:", typeof(StringHashAnonymizer), false)]
        public string AccessPointBSSID
        {
            get { return _accessPointBSSID; }
            set { _accessPointBSSID = value; }
        }

        public override string DisplayDetail
        {
            get { return "AP BSSID:  " + _accessPointBSSID; }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private WlanDatum() { }

        public WlanDatum(DateTimeOffset timestamp, string accessPointBSSID)
            : base(timestamp)
        {
            _accessPointBSSID = accessPointBSSID == null ? "" : accessPointBSSID;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "AP BSSID:  " + _accessPointBSSID;
        }
    }
}
