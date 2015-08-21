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

namespace SensusService.Probes.Communication
{
    public class TelephonyDatum : Datum
    {
        private TelephonyState _state;
        private string _phoneNumber;

        [ListProbeTriggerProperty(new object[] { TelephonyState.Idle, TelephonyState.IncomingCall, TelephonyState.OutgoingCall })]
        public TelephonyState State
        {
            get { return _state; }
            set { _state = value; }
        }

        [TextProbeTriggerProperty("Phone #")]
        [Anonymizable("Phone #:", typeof(StringHashAnonymizer), true)]
        public string PhoneNumber
        {
            get { return _phoneNumber; }
            set { _phoneNumber = value; }
        }

        public override string DisplayDetail
        {
            get { return _phoneNumber + " (" + _state + ")"; }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private TelephonyDatum() { }

        public TelephonyDatum(DateTimeOffset timestamp, TelephonyState state, string phoneNumber)
            : base(timestamp)
        {
            _state = state;
            _phoneNumber = phoneNumber == null ? "" : phoneNumber;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "State:  " + _state + Environment.NewLine +
                   "Number:  " + _phoneNumber;
        }
    }
}
