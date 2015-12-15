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
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace SensusUI.Inputs
{
    public abstract class Input
    {
        private string _name;
        private string _id;
        private string _groupId;
        private string _labelText;
        private int _labelFontSize;
        private View _view;
        private bool _displayNumber;
        private bool _complete;
        private bool _shouldBeStored;
        private double? _latitude;
        private double? _longitude;
        private DateTimeOffset? _locationUpdateTimestamp;
        private bool _required;
        private bool _viewed;
        private DateTimeOffset? _completionTimestamp;
        private List<InputDisplayCondition> _displayConditions;

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

        public int LabelFontSize
        {
            get
            {
                return _labelFontSize;
            }
            set
            {
                _labelFontSize = value;
            }
        }

        public bool DisplayNumber
        {
            get
            {
                return _displayNumber;
            }
            set
            {
                _displayNumber = value;
            }
        }

        [JsonIgnore]
        public abstract object Value { get; }

        [JsonIgnore]
        public bool Complete
        {
            get
            {
                return _complete || _viewed && !_required;
            }
            protected set
            {
                _complete = value; 

                if (_complete)
                    _completionTimestamp = DateTimeOffset.UtcNow;
                else
                    _completionTimestamp = null;
            }
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

        public DateTimeOffset? LocationUpdateTimestamp
        {
            get
            {
                return _locationUpdateTimestamp;
            }
            set
            {
                _locationUpdateTimestamp = value;
            }
        }

        [JsonIgnore]
        public abstract bool Enabled { get; set; }

        [JsonIgnore]
        public abstract string DefaultName { get; }

        [OnOffUiProperty(null, true, 5)]
        public bool Required
        {
            get
            {
                return _required;
            }
            set
            {
                _required = value;
            }
        }

        public bool Viewed
        {
            get
            {
                return _viewed;
            }
            set
            {
                _viewed = value;
            }
        }

        [JsonIgnore]
        public DateTimeOffset? CompletionTimestamp
        {
            get
            {
                return _completionTimestamp; 
            }
        }

        public List<InputDisplayCondition> DisplayConditions
        {
            get
            {
                return _displayConditions;
            }
        }

        [JsonIgnore]
        public bool ShouldBeDisplayed
        {
            get
            {
                List<InputDisplayCondition> conjuncts = _displayConditions.Where(displayCondition => displayCondition.Conjunction).ToList();
                if (conjuncts.Count > 0 && conjuncts.Any(displayCondition => !displayCondition.Satisfied))
                    return false;

                List<InputDisplayCondition> disjuncts = _displayConditions.Where(displayCondition => !displayCondition.Conjunction).ToList();
                if (disjuncts.Count > 0 && disjuncts.All(displayCondition => !displayCondition.Satisfied))
                    return false;

                return true;
            }
        }

        public Input()
        {
            _name = DefaultName;
            _id = Guid.NewGuid().ToString();
            _displayNumber = true;
            _complete = false;
            _shouldBeStored = true;
            _required = true;
            _viewed = false;
            _completionTimestamp = null;
            _labelFontSize = 20;
            _displayConditions = new List<InputDisplayCondition>();
        }

        public Input(string labelText)
            : this()
        {
            _labelText = labelText;
        }

        public Input(string labelText, int labelFontSize)
            : this(labelText)
        {
            _labelFontSize = labelFontSize;
        }

        public Input(string name, string labelText)
            : this(labelText)
        {
            _name = name;
        }

        protected Label CreateLabel(int index)
        {
            return new Label
            {
                Text = string.IsNullOrWhiteSpace(_labelText) ? "" : (_required ? "*" : "") + (index > 0 && _displayNumber ? index + ") " : "") + _labelText,
                FontSize = _labelFontSize

                // set the style ID on the label so that we can retrieve it when unit testing
                #if UNIT_TESTING
                , StyleId = Name + " Label"
                #endif
            };
        }

        public virtual View GetView(int index)
        {
            return _view;
        }

        protected virtual void SetView(View value)
        {
            _view = value; 
        }

        public void Reset()
        {            
            _view = null;
            _complete = false;
            _shouldBeStored = true;
            _latitude = null;
            _longitude = null;
            _locationUpdateTimestamp = null;
            _viewed = false;
            _completionTimestamp = null;
        }

        public override string ToString()
        {
            return _name + (_name == DefaultName ? "" : " -- " + DefaultName) + (_required ? "*" : "");
        }
    }
}