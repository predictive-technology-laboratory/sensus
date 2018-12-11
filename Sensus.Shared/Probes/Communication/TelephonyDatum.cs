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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Communication
{
    /// <summary>
    /// Represents phone call meta-data in terms of the phones <see cref="TelephonyState"/>, the other phone number, and -- if
    /// a call just ended -- the duration of the call. Both <see cref="TelephonyState.IncomingCall"/> and <see cref="TelephonyState.OutgoingCall"/>
    /// values will be associated with an unspecified <see cref="CallDurationSeconds"/>, as the former are recorded to mark the time
    /// at which the incoming call arrived or went out, respectively. When a call ends and the phone returns to <see cref="TelephonyState.Idle"/>, then
    /// there will be a value for <see cref="CallDurationSeconds"/> indicating how long the call lasted.
    /// </summary>
    public class TelephonyDatum : Datum, ITelephonyDatum
    {
        private TelephonyState _state;
        private string _phoneNumber;
        private double? _callDurationSeconds;

        /// <summary>
        /// The duration of the call. Note that this includes the time spent ringing.
        /// </summary>
        /// <value>The call duration seconds.</value>
        [DoubleProbeTriggerProperty("Call Duration (Secs.)")]
        public double? CallDurationSeconds
        {
            get { return _callDurationSeconds; }
            set { _callDurationSeconds = value; }
        }

        [ListProbeTriggerProperty(new object[] { TelephonyState.Idle, TelephonyState.IncomingCall, TelephonyState.OutgoingCall })]
        [JsonConverter(typeof(StringEnumConverter))]
        public TelephonyState State
        {
            get { return _state; }
            set { _state = value; }
        }

        [StringProbeTriggerProperty("Phone #")]
        [Anonymizable("Phone #:", typeof(StringHashAnonymizer), false)]
        public string PhoneNumber
        {
            get { return _phoneNumber; }
            set { _phoneNumber = value; }
        }

        public override string DisplayDetail
        {
            get { return _phoneNumber + " (" + _state + (_callDurationSeconds == null ? "" : ", Prior Call:  " + Math.Round(_callDurationSeconds.GetValueOrDefault(), 1) + "s") + ")"; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the phone number.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _phoneNumber;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private TelephonyDatum()
        {
        }

        public TelephonyDatum(DateTimeOffset timestamp, TelephonyState state, string phoneNumber, double? callDurationSeconds)
            : base(timestamp)
        {
            _state = state;
            _phoneNumber = phoneNumber == null ? "" : phoneNumber;
            _callDurationSeconds = callDurationSeconds;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "State:  " + _state + Environment.NewLine +
            "Number:  " + _phoneNumber +
            (_callDurationSeconds == null ? "" : Environment.NewLine +
            "Duration (Secs.):  " + _callDurationSeconds.GetValueOrDefault());
        }
    }
}
