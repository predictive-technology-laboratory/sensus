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
    /// Records state transitions for each <see cref="Script"/> as it progresses from delivery to the user through removal from 
    /// the system, either by the user completing and submitting it or deleting it without submission. This data stream in some
    /// ways parallels that of <see cref="ScriptDatum"/>, with the latter tracking data resulting from submission of the
    /// <see cref="Script"/> by the user. The <see cref="ScriptStateDatum"/> stream will be generated and submitted to the
    /// <see cref="DataStores.Remote.RemoteDataStore"/> regardless of whether the user submits the <see cref="Script"/>.
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
        /// The <see cref="ScriptState"/> of the <see cref="Script"/> over the course of its life cycle.
        /// </summary>
        /// <value>The state.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ScriptState State
        {
            get { return _state; }
            set { _state = value; }
        }

        /// <summary>
        /// See <see cref="ScriptDatum.ScriptId"/>.
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
        /// See <see cref="ScriptDatum.ScriptName"/>
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
        /// See <see cref="ScriptDatum.RunId"/>.
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
        /// Each <see cref="Script"/> is either triggered by another <see cref="Probe"/> or is triggered on a schedule. In
        /// the latter case, this field indicates when the <see cref="Script"/> was scheduled to run.
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
        /// See <see cref="ScriptDatum.TriggerDatumId"/>.
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

            // set properties that are usually set when a datum is stored by a probe. we don't
            // store the script state datum in a probe, but rather write it directly to the local
            // data store.
            ProtocolId = script.Runner.Probe.Protocol.Id;
            ParticipantId = script.Runner.Probe.Protocol.ParticipantId;
        }
    }
}