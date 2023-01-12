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
using System.Collections;
using System.Collections.Generic;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using Sensus.UI.Inputs;

namespace Sensus.Probes.User.Scripts
{
	/// <summary>
	/// The <see cref="Datum.Timestamp"/> field of a <see cref="ScriptDatum"/> indicates the time when the particular input (e.g., text box) was 
	/// completed by the user. Compare this with <see cref="LocationTimestamp"/>, <see cref="RunTimestamp"/>, and <see cref="SubmissionTimestamp"/>.
	/// 
	/// When a user submits a survey, a <see cref="ScriptDatum"/> object is submitted for each input in the survey (e.g., each text entry, 
	/// multiple-choice item, etc.). However, if the user does not submit the survey, no such objects will be submitted. As a means of tracking 
	/// the deployment and response/non-response of surveys, Sensus also submits additional <see cref="ScriptStateDatum"/>  objects to track
	/// the lifecycle of each <see cref="Script"/>.
	/// </summary>
	public class ScriptDatum : Datum
	{
		private string _scriptId;
		private string _scriptName;
		private string _groupId;
		private string _inputId;
		private string _runId;
		private string _inputLabel;
		private string _inputName;
		private object _response;
		private string _triggerDatumId;
		private double? _latitude;
		private double? _longitude;
		private DateTimeOffset _runTimestamp;
		private DateTimeOffset? _locationTimestamp;
		private List<InputCompletionRecord> _completionRecords;
		private DateTimeOffset _submissionTimestamp;

		/// <summary>
		/// Identifier for the <see cref="Script"/> within the <see cref="Protocol"/> that produced this <see cref="ScriptDatum"/>. 
		/// This identifier does not change across invocations of the <see cref="Script"/>. Compare this with <see cref="RunId"/>, 
		/// which identifies a particular invocation of a <see cref="Script"/>.
		/// </summary>
		/// <value>The script identifier.</value>
		public string ScriptId
		{
			get
			{
				return _scriptId;
			}
			set
			{
				_scriptId = value;
			}
		}

		/// <summary>
		/// Descriptive name for the <see cref="Script"/> that generated this <see cref="ScriptDatum"/>.
		/// </summary>
		/// <value>The name of the script.</value>
		public string ScriptName
		{
			get
			{
				return _scriptName;
			}
			set
			{
				_scriptName = value;
			}
		}

		/// <summary>
		/// Identifier for the <see cref="InputGroup"/> containing the <see cref="Input"/> that generated
		/// this <see cref="ScriptDatum"/>. In the UI, an <see cref="InputGroup"/> is rendered as a page
		/// of survey items. This identifier does not change across invocations of the <see cref="Script"/>.
		/// </summary>
		/// <value>The group identifier.</value>
		public string GroupId
		{
			get
			{
				return _groupId;
			}
			set
			{
				_groupId = value;
			}
		}

		/// <summary>
		/// Identifier for the <see cref="Input"/> that generated this <see cref="ScriptDatum"/>. This 
		/// identifier does not change across invocations of the <see cref="Script"/>.
		/// </summary>
		/// <value>The input identifier.</value>
		public string InputId
		{
			get
			{
				return _inputId;
			}
			set
			{
				_inputId = value;
			}
		}

		/// <summary>
		/// Name of the <see cref="Input"/> that generated this <see cref="ScriptDatum"/>. 
		/// </summary>
		/// <value>The input name.</value>
		public string InputName
		{
			get
			{
				return _inputName;
			}
			set
			{
				_inputName = value;
			}
		}

		/// <summary>
		/// Identifier for a particular invocation of a <see cref="Script"/>. This identifier changes for 
		/// each new invocation of a <see cref="Script"/>. Use this identifier to tie together all 
		/// generated <see cref="ScriptDatum"/> values for a single run of the <see cref="Script"/>.
		/// </summary>
		/// <value>The run identifier.</value>
		public string RunId
		{
			get
			{
				return _runId;
			}
			set
			{
				_runId = value;
			}
		}

		/// <summary>
		/// User's response to an <see cref="Input"/> within a particular invocation of a <see cref="Script"/>. The <see cref="Response"/>
		/// will be empty (null) when the user does not respond to the <see cref="Input"/>. Even when <see cref="Input.Required"/>
		/// is enabled, the user is still allowed to skip the <see cref="Input"/> after a warning message, resulting in a null 
		/// <see cref="Response"/>. The only way to ensure a non-empty <see cref="Response"/> is to enable 
		/// <see cref="InputGroup.ForceValidInputs"/>; however, this is not generally recommended as it is bad practice to lock user's
		/// into particular UI screens.
		/// </summary>
		/// <value>The response.</value>
		public object Response
		{
			get { return _response; }
			set { _response = value; }
		}

		/// <summary>
		/// If the <see cref="Script"/> is triggered by a <see cref="Datum"/> from another <see cref="Probe"/>, then this is the <see cref="Datum.Id"/>
		/// of the triggering <see cref="Datum"/>.
		/// </summary>
		/// <value>The trigger datum identifier.</value>
		[Anonymizable("Triggering Datum ID:", typeof(StringHashAnonymizer), false)]
		public string TriggerDatumId
		{
			get { return _triggerDatumId; }
			set { _triggerDatumId = value; }
		}

		/// <summary>
		/// Latitude of GPS reading taken when user submitted the <see cref="Script"/> (if enabled).
		/// </summary>
		/// <value>The latitude.</value>
		[DoubleProbeTriggerProperty]
		[Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
		public double? Latitude
		{
			get { return _latitude; }
			set { _latitude = value; }
		}

		/// <summary>
		/// Longitude of GPS reading taken when user submitted the <see cref="Script"/> (if enabled).
		/// </summary>
		/// <value>The longitude.</value>
		[DoubleProbeTriggerProperty]
		[Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
		public double? Longitude
		{
			get { return _longitude; }
			set { _longitude = value; }
		}

		/// <summary>
		/// Timestamp of GPS reading (if enabled).
		/// </summary>
		/// <value>The location timestamp.</value>
		public DateTimeOffset? LocationTimestamp
		{
			get
			{
				return _locationTimestamp;
			}
			set
			{
				_locationTimestamp = value;
			}
		}

		/// <summary>
		/// Timestamp of when a <see cref="Script"/> was made available to the user for completion.
		/// </summary>
		/// <value>The run timestamp.</value>
		public DateTimeOffset RunTimestamp
		{
			get
			{
				return _runTimestamp;
			}
			set
			{
				_runTimestamp = value;
			}
		}

		/// <summary>
		/// A trace of user activity for the <see cref="Input"/> that generated this <see cref="Script"/>. This is
		/// enabled by setting <see cref="Input.StoreCompletionRecords"/>.
		/// </summary>
		/// <value>The completion records.</value>
		public List<InputCompletionRecord> CompletionRecords
		{
			get
			{
				return _completionRecords;
			}
			// need setter in order for anonymizer to pick up the property (only includes writable properties)
			set
			{
				_completionRecords = value;
			}
		}

		/// <summary>
		/// Timestamp of when the user tapped the Submit button on the survey form.
		/// </summary>
		/// <value>The submission timestamp.</value>
		public DateTimeOffset SubmissionTimestamp
		{
			get
			{
				return _submissionTimestamp;
			}

			set
			{
				_submissionTimestamp = value;
			}
		}


		public bool ManualRun { get; set; }



		/// <summary>
		/// Label of the <see cref="Input"/> that generated this <see cref="ScriptDatum"/>. This 
		/// label does not change across invocations of the <see cref="Script"/>.
		/// </summary>
		/// <value>The input label.</value>

		public string InputLabel
		{
			get
			{
				return _inputLabel;
			}
			set
			{
				_inputLabel = value;
			}
		}

		public override string DisplayDetail
		{
			get
			{
				if (_response == null)
				{
					return "No response.";
				}
				else
				{
					if (_response is IList)
					{
						IList responseList = _response as IList;
						return responseList.Count + " response" + (responseList.Count == 1 ? "" : "s") + ".";
					}
					else
					{
						return _response.ToString();
					}
				}
			}
		}

		/// <summary>
		/// Gets the string placeholder value, which is the user's response.
		/// </summary>
		/// <value>The string placeholder value.</value>
		public override object StringPlaceholderValue
		{
			get
			{
				return _response;
			}
		}

		/// <summary>
		/// For JSON deserialization.
		/// </summary>
		private ScriptDatum()
		{
			_completionRecords = new List<InputCompletionRecord>();
		}


		public ScriptDatum(DateTimeOffset timestamp, string scriptId, string scriptName, string groupId, string inputId, string runId, string inputLabel, string inputName, object response, string triggerDatumId, double? latitude, double? longitude, DateTimeOffset? locationTimestamp, DateTimeOffset runTimestamp, List<InputCompletionRecord> completionRecords, DateTimeOffset submissionTimestamp, bool manualRun)
			: base(timestamp)
		{
			_scriptId = scriptId;
			_scriptName = scriptName;
			_groupId = groupId;
			_inputId = inputId;
			_runId = runId;
			_inputLabel = inputLabel;
			_inputName = inputName;
			_response = response;
			_triggerDatumId = triggerDatumId == null ? "" : triggerDatumId;
			_latitude = latitude;
			_longitude = longitude;
			_locationTimestamp = locationTimestamp;
			_runTimestamp = runTimestamp;
			_completionRecords = completionRecords;
			_submissionTimestamp = submissionTimestamp;
			ManualRun = manualRun;
		}

		public override string ToString()
		{
			return base.ToString() + Environment.NewLine +
			"Script:  " + _scriptId + Environment.NewLine +
			"Group:  " + _groupId + Environment.NewLine +
			"Input:  " + _inputId + Environment.NewLine +
			"Run:  " + _runId + Environment.NewLine +
			"Input label" + _inputLabel + Environment.NewLine +
			"Input name" + _inputName + Environment.NewLine +
			"Response:  " + _response + Environment.NewLine +
			"Latitude:  " + _latitude + Environment.NewLine +
			"Longitude:  " + _longitude + Environment.NewLine +
			"Location Timestamp:  " + _locationTimestamp + Environment.NewLine +
			"Run Timestamp:  " + _runTimestamp + Environment.NewLine +
			"Submission Timestamp:  " + _submissionTimestamp;
		}
	}
}