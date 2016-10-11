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
using Sensus.Shared.Exceptions;
using Sensus.Shared.UI.UiProperties;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Sensus.Shared;
using Xamarin;

namespace Sensus.Shared.UI.Inputs
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
        private bool _needsToBeStored;
        private double? _latitude;
        private double? _longitude;
        private DateTimeOffset? _locationUpdateTimestamp;
        private bool _required;
        private bool _viewed;
        private DateTimeOffset? _completionTimestamp;
        private List<InputDisplayCondition> _displayConditions;
        private Color? _backgroundColor;
        private Thickness? _padding;
        private bool _frame;
        private List<InputCompletionRecord> _completionRecords;
        private DateTimeOffset? _submissionTimestamp;

        [EntryStringUiProperty("Name:", true, 0)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
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

        /// <summary>
        /// Gets or sets a value indicating whether the user has interacted with this <see cref="Input"/>,
        /// leaving it in a state of completion. Contrast with Valid, which merely indicates that the 
        /// state of the input will not prevent the user from moving through an input request (e.g., in the case
        /// of inputs that are not required).
        /// </summary>
        /// <value><c>true</c> if complete; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool Complete
        {
            get
            {
                return _complete;
            }
            protected set
            {
                _complete = value;

                DateTimeOffset timestamp = DateTimeOffset.UtcNow;
                object inputValue = null;
                _completionTimestamp = null;

                if (_complete)
                {
                    _completionTimestamp = timestamp;

                    // get a deep copy of the value. some inputs have list values, and simply using the list reference wouldn't track the history, since the most up-to-date list would be used for all history values.
                    inputValue = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(Value, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
                }

                if (StoreCompletionRecords)
                    _completionRecords.Add(new InputCompletionRecord(timestamp, inputValue));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Input"/> is valid. A valid input is one that
        /// is complete, one that has been viewed but is not required, or one that isn't displayed. In short, it is an
        /// input state that should not prevent the user from proceeding through an input request.
        /// </summary>
        /// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool Valid
        {
            get
            {
                return _complete || _viewed && !_required || !Display;
            }
        }

        public bool NeedsToBeStored
        {
            get
            {
                return _needsToBeStored;
            }
            set
            {
                _needsToBeStored = value;
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

        public Color? BackgroundColor
        {
            get
            {
                return _backgroundColor;
            }
            set
            {
                _backgroundColor = value;
            }
        }

        public Thickness? Padding
        {
            get
            {
                return _padding;
            }
            set
            {
                _padding = value;
            }
        }

        public bool Frame
        {
            get
            {
                return _frame;
            }
            set
            {
                _frame = value;
            }
        }

        public List<InputCompletionRecord> CompletionRecords
        {
            get
            {
                return _completionRecords;
            }
        }

        public virtual bool StoreCompletionRecords
        {
            get
            {
                return true;
            }
        }

        [JsonIgnore]
        public bool Display
        {
            get
            {
                List<InputDisplayCondition> conjuncts = _displayConditions.Where(displayCondition => displayCondition.Conjunctive).ToList();
                if (conjuncts.Count > 0 && conjuncts.Any(displayCondition => !displayCondition.Satisfied))
                    return false;

                List<InputDisplayCondition> disjuncts = _displayConditions.Where(displayCondition => !displayCondition.Conjunctive).ToList();
                if (disjuncts.Count > 0 && disjuncts.All(displayCondition => !displayCondition.Satisfied))
                    return false;

                return true;
            }
        }

        public DateTimeOffset? SubmissionTimestamp
        {
            get
            {
                return _submissionTimestamp;
            }

            set
            {
                _submissionTimestamp = value;
            }
        }

        public Input()
        {
            _name = DefaultName;
            _id = Guid.NewGuid().ToString();
            _displayNumber = true;
            _complete = false;
            _needsToBeStored = true;
            _required = true;
            _viewed = false;
            _completionTimestamp = null;
            _labelFontSize = 20;
            _displayConditions = new List<InputDisplayCondition>();
            _backgroundColor = null;
            _padding = null;
            _frame = true;
            _completionRecords = new List<InputCompletionRecord>();
            _submissionTimestamp = null;
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
                Text = GetLabelText(index),
                FontSize = _labelFontSize

                // set the style ID on the label so that we can retrieve it when unit testing
#if UNIT_TESTING
                , StyleId = Name + " Label"
#endif
            };
        }

        protected string GetLabelText(int index)
        {
            return string.IsNullOrWhiteSpace(_labelText) ? "" : (_required ? "*" : "") + (index > 0 && _displayNumber ? index + ") " : "") + _labelText;
        }

        public virtual View GetView(int index)
        {
            return _view;
        }

        protected virtual void SetView(View value)
        {
            ContentView viewContainer = new ContentView
            {
                Content = value
            };

            if (_backgroundColor != null)
                viewContainer.BackgroundColor = _backgroundColor.GetValueOrDefault();

            if (_padding != null)
                viewContainer.Padding = _padding.GetValueOrDefault();

            _view = viewContainer;
        }

        public void Reset()
        {
            _view                    = null;
            _complete                = false;
            _needsToBeStored         = true;
            _latitude                = null;
            _longitude               = null;
            _locationUpdateTimestamp = null;
            _viewed                  = false;
            _completionTimestamp     = null;
            _backgroundColor         = null;
            _padding                 = null;
        }

        public virtual bool ValueMatches(object conditionValue, bool conjunctive)
        {
            // if either is null, both must be null to be equal
            if (Value == null || conditionValue == null)
                return Value == null && conditionValue == null;
            // if they're of the same type, compare
            else if (Value.GetType().Equals(conditionValue.GetType()))
                return Value.Equals(conditionValue);
            else
            {
                // this should never happen
                try
                {
                    Insights.Report(new Exception("Called Input.ValueMatches with conditionValue of type " + conditionValue.GetType() + ". Comparing with value of type " + Value.GetType() + "."), Insights.Severity.Critical);
                }
                catch (Exception)
                {
                }

                return false;
            }
        }

        public override string ToString()
        {
            return _name + (_name == DefaultName ? "" : " -- " + DefaultName) + (_required ? "*" : "");
        }

        public Input Copy()
        {
            Input copy = JsonConvert.DeserializeObject<Input>(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);

            copy.Reset();

            return copy;
        }
    }
}