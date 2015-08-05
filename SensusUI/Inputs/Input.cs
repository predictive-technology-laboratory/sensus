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
using Xamarin.Forms;
using SensusService.Exceptions;
using SensusUI.UiProperties;
using Newtonsoft.Json;

namespace SensusUI.Inputs
{
    public abstract class Input
    {
        private string _name;
        private string _id;
        private string _groupId;
        private string _labelText;

        [EntryStringUiProperty("Name:", true, 0)]
        public string Name
        {
            get{ return _name; }
            set{ _name = value; }
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

        public string GroupId
        {
            get
            {
                return _groupId;
            }
            set
            {
                _groupId = value;
            }
        }

        [EntryStringUiProperty("Label Text:", true, 1)]
        public string LabelText
        {
            get
            {
                return _labelText;
            }
            set
            {
                _labelText = value;
            }
        }

        protected Label Label
        {
            get
            {
                return new Label
                {
                    Text = _labelText,
                    FontSize = 20
                };
            }
        }

        [JsonIgnore]
        public abstract bool Complete { get; }

        [JsonIgnore]
        public abstract string DisplayName { get; }

        public Input()
        {
            _name = DisplayName;
        }

        public Input(string name, string labelText)
        {
            _name = name;
            _labelText = labelText;
            _id = Guid.NewGuid().ToString();
        }

        public abstract View CreateView(out Func<object> valueRetriever);

        public override string ToString()
        {
            return _name + " (" + GetType().Name + ")";
        }
    }
}