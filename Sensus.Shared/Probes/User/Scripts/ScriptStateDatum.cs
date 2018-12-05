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