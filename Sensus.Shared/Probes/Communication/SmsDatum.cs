//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
