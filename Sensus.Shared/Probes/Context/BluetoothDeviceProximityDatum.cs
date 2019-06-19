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

namespace Sensus.Probes.Context
{
    public class BluetoothDeviceProximityDatum : Datum, IBluetoothDeviceProximityDatum
    {
        private string _encounteredDeviceId;

        [StringProbeTriggerProperty]
        [Anonymizable("Encountered Device ID:", typeof(StringHashAnonymizer), false)]
        public string EncounteredDeviceId
        {
            get { return _encounteredDeviceId; }
            set { _encounteredDeviceId = value; }
        }

		[Anonymizable("Encountered Device Address:", typeof(StringHashAnonymizer), false)]
		public string Address { get; set; }
		[Anonymizable("Encountered Device Name:", typeof(StringHashAnonymizer), false)]
		public string Name { get; set; }
		public int Rssi { get; set; }
		public bool Paired { get; set; }
		public bool RunningSensus { get; set; }

        public override string DisplayDetail
        {
            get { return _encounteredDeviceId; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the encountered device ID.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _encounteredDeviceId;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private BluetoothDeviceProximityDatum() { }

        public BluetoothDeviceProximityDatum(DateTimeOffset timestamp, string encounteredDeviceId, string address, string name, int rssi, bool paired, bool runningSensus)
            : base(timestamp)
        {
            _encounteredDeviceId = encounteredDeviceId;

			Address = address;
			Name = name;
			Rssi = rssi;
			Paired = paired;
			RunningSensus = RunningSensus;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Encountered device ID:  " + _encounteredDeviceId;
        }
    }
}
