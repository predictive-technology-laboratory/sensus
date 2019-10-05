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
		private bool? _isContact;
		private string _name;
		private string _email;

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

		/// <summary>
		/// Indicates whether the other participant is a contact.
		/// </summary>
		[BooleanProbeTriggerProperty]
		[Anonymizable("Sender/receipient is in contacts:", null, false)]
		public bool? IsContact
		{
			get { return _isContact; }
			set { _isContact = value; }
		}

		/// <summary>
		/// The name of the other participant.
		/// </summary>
		[StringProbeTriggerProperty]
		[Anonymizable("Sender/receipient's name:", null, false)]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		/// <summary>
		/// The email address of the other participant.
		/// </summary>
		[StringProbeTriggerProperty]
		[Anonymizable("Sender/receipient's email:", null, false)]
		public string Email
		{
			get { return _email; }
			set { _email = value; }
		}

		/// <summary>
		/// The current state of the call.
		/// </summary>
		[ListProbeTriggerProperty(new object[] { TelephonyState.Idle, TelephonyState.IncomingCall, TelephonyState.OutgoingCall })]
		[JsonConverter(typeof(StringEnumConverter))]
		public TelephonyState State
		{
			get { return _state; }
			set { _state = value; }
		}

		/// <summary>
		/// The phone number that was called.
		/// </summary>
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

		public TelephonyDatum(DateTimeOffset timestamp, TelephonyState state, string phoneNumber, double? callDurationSeconds, bool? isContact, string name, string email)
			: base(timestamp)
		{
			_state = state;
			_phoneNumber = phoneNumber == null ? "" : phoneNumber;
			_callDurationSeconds = callDurationSeconds;
			_isContact = isContact;
			_name = name;
			_email = email;
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