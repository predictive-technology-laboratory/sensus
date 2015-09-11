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
        private View _view;
        private bool _complete;
        private bool _shouldBeStored;
        private double? _latitude;
        private double? _longitude;

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
        public virtual View View
        {
            get { return _view; }
            protected set { _view = value; }
        }

        [JsonIgnore]
        public abstract object Value { get; }

        [JsonIgnore]
        public bool Complete
        {
            get { return _complete; }
            protected set { _complete = value; }
        }

        public bool ShouldBeStored
        {
            get
            {
                return _shouldBeStored;
            }
            set
            {
                _shouldBeStored = value;
            }
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

        [JsonIgnore]
        public abstract bool Enabled { get; set; }

        [JsonIgnore]
        public abstract string DefaultName { get; }

        public Input()
        {
            _name = DefaultName;
            _id = Guid.NewGuid().ToString();
            _shouldBeStored = true;
        }

        public Input(string labelText)
            : this()
        {
            _labelText = labelText;
        }

        public Input(string name, string labelText)
            : this(labelText)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name + (_name == DefaultName ? "" : " -- " + DefaultName);
        }
    }
}