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
    /// the deployment and response/non-response of surveys, Sensus also submits additional <see cref="ScriptStateDatum"/>  objects.
    /// 
    /// </summary>
    public class ScriptDatum : Datum
    {
        private string _scriptId;
        private string _scriptName;
        private string _groupId;
        private string _inputId;
        private string _runId;
        private object _response;
        private string _triggerDatumId;
        private double? _latitude;
        private double? _longitude;
        private DateTimeOffset _runTimestamp;
        private DateTimeOffset? _locationTimestamp;
        private List<InputCompletionRecord> _completionRecords;
        private DateTimeOffset _submissionTimestamp;

        /// <summary>
        /// Identifier for a script. This does not change across invocations of the script.
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
        /// Descriptive name for a script.
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
        /// Identifier for a set of inputs within the script. This does not change across invocations of the script.
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
        /// Identifier for an input within the script. This does not change across invocations of the script.
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
        /// Identifier for a particular invocation of a script. This changes for each new invocation of the script.
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
        /// User's response to an input within the script.
        /// </summary>
        /// <value>The response.</value>
        public object Response
        {
            get { return _response; }
            set { _response = value; }
        }

        /// <summary>
        /// If the script is triggered by a <see cref="Datum"/> from another probe, this is the <see cref="Datum.Id"/>.
        /// </summary>
        /// <value>The trigger datum identifier.</value>
        [Anonymizable("Triggering Datum ID:", typeof(StringHashAnonymizer), false)]
        public string TriggerDatumId
        {
            get { return _triggerDatumId; }
            set { _triggerDatumId = value; }
        }

        /// <summary>
        /// Latitude of GPS reading taken when user submitted the response (if enabled).
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
        /// Longitude of GPS reading taken when user submitted the response (if enabled).
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
        /// Timestamp of when a script survey was made available to the user for completion.
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
        /// A trace of activity for the input.
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

        public ScriptDatum(DateTimeOffset timestamp, string scriptId, string scriptName, string groupId, string inputId, string runId, object response, string triggerDatumId, double? latitude, double? longitude, DateTimeOffset? locationTimestamp, DateTimeOffset runTimestamp, List<InputCompletionRecord> completionRecords, DateTimeOffset submissionTimestamp)
            : base(timestamp)
        {
            _scriptId = scriptId;
            _scriptName = scriptName;
            _groupId = groupId;
            _inputId = inputId;
            _runId = runId;
            _response = response;
            _triggerDatumId = triggerDatumId == null ? "" : triggerDatumId;
            _latitude = latitude;
            _longitude = longitude;
            _locationTimestamp = locationTimestamp;
            _runTimestamp = runTimestamp;
            _completionRecords = completionRecords;
            _submissionTimestamp = submissionTimestamp;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Script:  " + _scriptId + Environment.NewLine +
            "Group:  " + _groupId + Environment.NewLine +
            "Input:  " + _inputId + Environment.NewLine +
            "Run:  " + _runId + Environment.NewLine +
            "Response:  " + _response + Environment.NewLine +
            "Latitude:  " + _latitude + Environment.NewLine +
            "Longitude:  " + _longitude + Environment.NewLine +
            "Location Timestamp:  " + _locationTimestamp + Environment.NewLine + 
            "Run Timestamp:  " + _runTimestamp + Environment.NewLine +
            "Submission Timestamp:  " + _submissionTimestamp;
        }
    }
}
