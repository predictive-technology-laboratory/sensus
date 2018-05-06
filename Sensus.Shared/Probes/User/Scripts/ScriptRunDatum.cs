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

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// Note:  The <see cref="Datum.Timestamp"/> field indicates the time when the script survey was made available to the user for 
    /// completion. It will equal <see cref="Datum.Timestamp"/> for any <see cref="ScriptDatum"/>  objects that end up being 
    /// submitted by the user.
    /// </summary>
    public class ScriptRunDatum : Datum
    {
        private string _scriptId;
        private string _scriptName;
        private string _runId;
        private DateTimeOffset? _scheduledTimestamp;
        private string _triggerDatumId;
        private double? _latitude;
        private double? _longitude;
        private DateTimeOffset? _locationTimestamp;

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

        /// <summary>
        /// Latitude of GPS reading taken when the survey was displayed to the user (if enabled).
        /// </summary>
        /// <value>The latitude.</value>
        public double? Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        /// <summary>
        /// Longitude of GPS reading taken when the survey was displayed to the user (if enabled).
        /// </summary>
        /// <value>The longitude.</value>
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

        public override string DisplayDetail
        {
            get
            {
                return "Script ran.";
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

        public ScriptRunDatum(DateTimeOffset timestamp, string scriptId, string scriptName, string runId, DateTimeOffset? scheduledTimestamp, string triggerDatumId, double? latitude, double? longitude, DateTimeOffset? locationTimestamp)
            : base(timestamp)
        {
            _scriptId = scriptId;
            _scriptName = scriptName;
            _runId = runId;
            _scheduledTimestamp = scheduledTimestamp;
            _triggerDatumId = triggerDatumId == null ? "" : triggerDatumId;
            _latitude = latitude;
            _longitude = longitude;
            _locationTimestamp = locationTimestamp;
        }
    }
}