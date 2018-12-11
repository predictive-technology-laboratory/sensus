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

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// Records state updates for each <see cref="Script"/> as it progresses from delivery through removal from the system.
    /// </summary>
    public class ScriptStateDatum : Datum
    {
        private ScriptState _state;
        private string _scriptId;
        private string _scriptName;
        private string _runId;
        private DateTimeOffset? _scheduledTimestamp;
        private string _triggerDatumId;

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ScriptState State
        {
            get { return _state; }
            set { _state = value; }
        }

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
        /// Some scripts are triggered by other probes, and some scripts are scheduled. If the script is of the latter type, 
        /// this field indicates when the script was scheduled to run.
        /// </summary>
        /// <value>The scheduled timestamp.</value>
        public DateTimeOffset? ScheduledTimestamp
        {
            get
            {
                return _scheduledTimestamp;
            }
            set
            {
                _scheduledTimestamp = value;
            } 
        }

        /// <summary>
        /// If the script is triggered by a Datum from another probe, this is the Id of that datum.
        /// </summary>
        /// <value>The trigger datum identifier.</value>
        public string TriggerDatumId
        {
            get { return _triggerDatumId; }
            set { _triggerDatumId = value; }
        }

        public override string DisplayDetail
        {
            get
            {
                return "State:  " + _state;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is always blank.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return "";
            }
        }

        public ScriptStateDatum(ScriptState state, DateTimeOffset timestamp, Script script)
            : base(timestamp)
        {
            _state = state;
            _scriptId = script.Runner.Script.Id;
            _scriptName = script.Runner.Name;
            _runId = script.Id;
            _scheduledTimestamp = script.ScheduledRunTime;
            _triggerDatumId = script.CurrentDatum?.Id ?? "";
        }
    }
}
