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
using System.Text.RegularExpressions;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Communication
{
    public class SmsDatum : Datum, ISmsDatum
    {
        private string _fromNumber;
        private string _toNumber;
        private string _message;
        private bool _participantIsSender;

        [StringProbeTriggerProperty("From #")]
        [Anonymizable("From #:", typeof(StringHashAnonymizer), false)]
        public string FromNumber
        {
            get { return _fromNumber; }
            set { _fromNumber = value == null ? "" : new Regex(@"[^0-9]").Replace(value, ""); }
        }

        [StringProbeTriggerProperty("To #")]
        [Anonymizable("To #:", typeof(StringHashAnonymizer), false)]
        public string ToNumber
        {
            get { return _toNumber; }
            set { _toNumber = value == null ? "" : new Regex(@"[^0-9]").Replace(value, ""); }
        }

        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        [BooleanProbeTriggerProperty]
        [Anonymizable("Participant Is Sender:", null, false)]
        public bool ParticipantIsSender
        {
            get
            {
                return _participantIsSender;
            }

            set
            {
                _participantIsSender = value;
            }
        }

        public override string DisplayDetail
        {
            get { return _message; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the SMS message.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _message;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private SmsDatum()
        {
        }

        public SmsDatum(DateTimeOffset timestamp, string fromNumber, string toNumber, string message, bool participantIsSender)
            : base(timestamp)
        {
            FromNumber = fromNumber == null ? "" : fromNumber;
            ToNumber = toNumber == null ? "" : toNumber;
            _message = message == null ? "" : message;
            _participantIsSender = participantIsSender;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                       "From:  " + _fromNumber + Environment.NewLine +
                       "To:  " + _toNumber + Environment.NewLine +
                       "Message:  " + _message + Environment.NewLine +
                       "Participant is Sender:  " + _participantIsSender;
        }
    }
}
