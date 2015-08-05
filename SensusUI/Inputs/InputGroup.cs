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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SensusUI.UiProperties;

namespace SensusUI.Inputs
{
    public class InputGroup
    {
        private string _id;
        private string _name;
        private ObservableCollection<Input> _inputs;

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

        [EntryStringUiProperty(null, true, 0)]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public ObservableCollection<Input> Inputs
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
            _id = Guid.NewGuid().ToString();

            _inputs = new ObservableCollection<Input>();

            _inputs.CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    foreach (Input input in e.NewItems)
                        input.GroupId = _id;
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                    foreach (Input input in e.OldItems)
                        input.GroupId = null;
            };
        }

        public InputGroup(string name)
            : this()
        {
            _name = name;
        }

        public InputGroup(string name, Input input)
            : this(name)
        {
            _inputs.Add(input);
        }

        public override string ToString()
        {
            return _name;
        }
    }
}