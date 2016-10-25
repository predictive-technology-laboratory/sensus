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

namespace Sensus.Shared.Probes.User.Scripts
{
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

        public string TriggerDatumId
        {
            get { return _triggerDatumId; }
            set { _triggerDatumId = value; }
        }

        public double? Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        public double? Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }

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