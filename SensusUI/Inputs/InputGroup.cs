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
using System.Linq;

namespace SensusUI.Inputs
{
    public class InputGroup
    {
        private string _id;
        private List<Input> _inputs;

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

        public List<Input> Inputs
        {
            get { return _inputs; }
        }

        public bool Complete
        {
            get { return _inputs.All(i => i.Complete); }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        protected InputGroup()
        {
            _inputs = new List<Input>();
        }

        public InputGroup(string id)
            : this()
        {
            _id = id;
        }

        public InputGroup(string id, Input input)
            : this(id)
        {
            AddInput(input);
        }

        public void AddInput(Input input)
        {
            input.GroupId = _id;

            _inputs.Add(input);
        }

        public bool RemoveInput(Input input)
        {
            input.GroupId = null;

            return _inputs.Remove(input);
        }

        public void RunAsync()
        {
        }
    }
}