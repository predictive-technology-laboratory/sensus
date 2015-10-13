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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using SensusUI.Inputs;

namespace SensusService.Probes.User
{
    public class Script
    {
        private ObservableCollection<InputGroup> _inputGroups;
        private DateTimeOffset _firstRunTimestamp;
        private Datum _previousDatum;
        private Datum _currentDatum;

        public ObservableCollection<InputGroup> InputGroups
        {
            get { return _inputGroups; }
        }

        public DateTimeOffset FirstRunTimestamp
        {
            get { return _firstRunTimestamp; }
            set { _firstRunTimestamp = value; }
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
        public bool Complete
        {
            get { return _inputGroups.Count == 0 || _inputGroups.All(inputGroup => inputGroup.Complete); }
        }

        [JsonIgnore]
        public TimeSpan Age
        {
            get { return DateTimeOffset.UtcNow - _firstRunTimestamp; }
        }

        public Script()
        {                        
            _inputGroups = new ObservableCollection<InputGroup>();
        }

        public Script Copy()
        {
            return JsonConvert.DeserializeObject<Script>(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
        }
    }
}