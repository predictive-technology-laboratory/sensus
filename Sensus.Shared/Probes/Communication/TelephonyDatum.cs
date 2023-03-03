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
	public class TelephonyDatum : Datum, ITelephonyDatum
	{
		/// <summary>
		/// The current state of the call.
		/// </summary>
		[ListProbeTriggerProperty(new object[] { TelephonyState.Idle, TelephonyState.Ringing, TelephonyState.OffHook })]
		[JsonConverter(typeof(StringEnumConverter))]
		public TelephonyState State { get; set; }

		public override string DisplayDetail => State.ToString();

		/// <summary>
		/// Gets the string placeholder value.
		/// </summary>
		/// <value>The string placeholder value.</value>
		public override object StringPlaceholderValue => State.ToString();

		/// <summary>
		/// For JSON deserialization.
		/// </summary>
		private TelephonyDatum()
		{

		}

		public TelephonyDatum(DateTimeOffset timestamp, TelephonyState state) : base(timestamp)
		{
			State = state;
		}

		public override string ToString()
		{
			return State.ToString();
		}
	}
}