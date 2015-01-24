#region copyright
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
#endregion

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SensusService.Probes.User
{
    public class Script
    {
        #region static members
        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static Script FromJSON(string json)
        {
            return JsonConvert.DeserializeObject<Script>(json, _jsonSerializerSettings);
        }
        #endregion

        private string _id;
        private int _hashCode;
        private string _name;
        private int _delayMS;
        private List<Prompt> _prompts;
        private bool _hasRun;
        private DateTimeOffset _firstRunTimeStamp;
        private Datum _previousDatum;
        private Datum _currentDatum;

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;

                if (_id == null)
                    _hashCode = base.GetHashCode();
                else
                    _hashCode = _id.GetHashCode();
            }
        } 

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int DelayMS
        {
            get { return _delayMS; }
            set { _delayMS = value; }
        }

        public List<Prompt> Prompts
        {
            get { return _prompts; }
            set { _prompts = value; }
        }

        public bool HasRun
        {
            get { return _hasRun; }
            set { _hasRun = value; }
        }

        public DateTimeOffset FirstRunTimeStamp
        {
            get { return _firstRunTimeStamp; }
            set { _firstRunTimeStamp = value; }
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
            get { return _hasRun && _prompts.Count == 0 ? true : _prompts.All(p => p.MostRecentInputDatum != null); }
        }

        /// <summary>
        /// Constructor for JSON deserialization.
        /// </summary>
        private Script()
        {
            _id = Guid.NewGuid().ToString();
            _hashCode = _id.GetHashCode();
            _delayMS = 0;
            _prompts = new List<Prompt>();
            _hasRun = false;
        }

        public Script(string name)
            : this()
        {
            _name = name;
        }

        public Script(string name, int delayMS)
            : this(name)
        {
            _delayMS = delayMS;
        }

        public void Save(string path)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                file.Write(JsonConvert.SerializeObject(this, _jsonSerializerSettings));
                file.Close();
            }
        }

        public Task<List<ScriptDatum>> RunAsync(Datum previousDatum, Datum currentDatum)
        {
            SensusServiceHelper.Get().Logger.Log("Running script \"" + _name + "\".", LoggingLevel.Normal);

            if (previousDatum != null)
                _previousDatum = previousDatum;

            if (currentDatum != null)
                _currentDatum = currentDatum;

            return Task.Run<List<ScriptDatum>>(async () =>
                {
                    if (_delayMS > 0)
                        Thread.Sleep(_delayMS);

                    if (!_hasRun)
                        _firstRunTimeStamp = DateTimeOffset.UtcNow;

                    List<ScriptDatum> scriptData = new List<ScriptDatum>();

                    foreach (Prompt prompt in _prompts)
                        if (prompt.MostRecentInputDatum == null)
                        {
                            ScriptDatum scriptDatum = await prompt.RunAsync(_previousDatum, _currentDatum);
                            if (scriptDatum != null)
                                scriptData.Add(scriptDatum);
                        }

                    _hasRun = true;

                    SensusServiceHelper.Get().Logger.Log("Script \"" + _name + "\" has finished running.", LoggingLevel.Normal);

                    return scriptData;
                });
        }

        public Script Copy()
        {
            return JsonConvert.DeserializeObject<Script>(JsonConvert.SerializeObject(this, _jsonSerializerSettings));
        }

        public override bool Equals(object obj)
        {
            Script s = obj as Script;

            return s != null && _id == s._id;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
