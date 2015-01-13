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
using System.Collections.Generic;
using System.IO;

namespace SensusService.Probes.User
{
    public class Script
    {
        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static Script FromJSON(string json)
        {
            return JsonConvert.DeserializeObject<Script>(json, _jsonSerializerSettings);
        }

        private string _name;
        private Prompt[] _prompts;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Prompt[] Prompts
        {
            get { return _prompts; }
            set { _prompts = value; }
        }

        private Script() { }  // for JSON deserialization

        public Script(string name, params Prompt[] prompts)
        {
            _name = name;
            _prompts = prompts;
        }

        public void Save(string path)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                file.Write(JsonConvert.SerializeObject(this, _jsonSerializerSettings));
                file.Close();
            }
        }

        public void Run(Datum previous, Datum current)
        {
            foreach (Prompt prompt in _prompts)
                prompt.Run();
        }
    }
}
