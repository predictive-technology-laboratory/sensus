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
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using SensusUI.Inputs;

namespace SensusService.Probes.User.Scripts
{
    public class Script
    {
        private ScriptRunner _runner;
        private ObservableCollection<InputGroup> _inputGroups;
        private DateTimeOffset? _runTimestamp;
        private Datum _previousDatum;
        private Datum _currentDatum;
        private string _id;

        public ScriptRunner Runner
        {
            get
            {
                return _runner;
            }
            set
            {
                _runner = value;
            }
        }

        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public ObservableCollection<InputGroup> InputGroups
        {
            get { return _inputGroups; }
        }

        public DateTimeOffset? RunTimestamp
        {
            get { return _runTimestamp; }
            set { _runTimestamp = value; }
        }

        public Datum PreviousDatum
        {
            get { return _previousDatum; }
            set { _previousDatum = value; }
        }

        public Datum CurrentDatum
        {
            get { return _currentDatum; }
            set { _currentDatum = value; }
        }

        [JsonIgnore]
        public bool Valid
        {
            get { return _inputGroups.Count == 0 || _inputGroups.All(inputGroup => inputGroup.Valid); }
        }

        [JsonIgnore]
        public TimeSpan Age
        {
            get
            {
                return DateTimeOffset.UtcNow - _runTimestamp.Value;
            }
        }

        public Script(ScriptRunner runner)
        {
            _runner = runner;
            _id = Guid.NewGuid().ToString();
            _inputGroups = new ObservableCollection<InputGroup>();
        }

        public bool SameOrigin(Script script)
        {
            return _runner.Script.Id == script.Runner.Script.Id;
        }

        public Script Copy()
        {
            // don't copy the runner
            ScriptRunner runner = _runner;
            _runner = null;

            Script copy = JsonConvert.DeserializeObject<Script>(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);

            // a new GUID is set within the constructor, but it is immediately overwritten with the old id by the JSON deserializer. since the
            // script id is what ties all of the script datum objects together, set a new unique script id here.
            copy.Id = Guid.NewGuid().ToString();

            // set the runner within the current and copied scripts
            _runner = copy.Runner = runner;

            return copy;
        }
    }
}