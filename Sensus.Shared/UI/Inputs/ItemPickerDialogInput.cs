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
using Sensus.UI.UiProperties;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Sensus.UI.Inputs
{
    public class ItemPickerDialogInput : ItemPickerInput, IVariableDefiningInput
    {
        private string _tipText;
        private List<string> _items;
        private bool _allowClearSelection;
        private Picker _picker;
        private Label _label;
        private string _definedVariable;

        /// <summary>
        /// A short tip that explains how to pick an item from the dialog window.
        /// </summary>
        /// <value>The tip text.</value>
        [EntryStringUiProperty("Tip Text:", true, 10)]
        public string TipText
        {
            get
            {
                return _tipText;
            }
            set
            {
                _tipText = value;
            }
        }

        /// <summary>
        /// These are the items that the user will have to select from.
        /// </summary>
        /// <value>The items.</value>
        [EditableListUiProperty(null, true, 11)]
        public List<string> Items
        {
            get
            {
                return _items;
            }
            // need set method so that binding can set the list via the EditableListUiProperty
            set
            {
                _items = value;
            }
        }

        /// <summary>
        /// Whether or not to allow the user to clear the current selection.
        /// </summary>
        /// <value><c>true</c> to allow clear selection; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Allow Clear Selection:", true, 12)]
        public bool AllowClearSelection
        {
            get
            {
                return _allowClearSelection;
            }
            set
            {
                _allowClearSelection = value;
            }
        }

        /// <summary>
        /// The name of the variable in <see cref="Protocol.VariableValueUiProperty"/> that this input should
        /// define the value for. For example, if you wanted this input to supply the value for a variable
        /// named `study-name`, then set this field to `study-name` and the user's selection will be used as
        /// the value for this variable. 
        /// </summary>
        /// <value>The defined variable.</value>
        [EntryStringUiProperty("Define Variable:", true, 13)]
        public string DefinedVariable
        {
            get
            {
                return _definedVariable;
            }
            set
            {
                _definedVariable = value?.Trim();
            }
        }

        public override object Value
        {
            get
            {
                return _picker == null || _picker.SelectedIndex < 0 ? null : _picker.Items[_picker.SelectedIndex];
            }
        }

        [JsonIgnore]
        public override bool Enabled
        {
            get
            {
                return _picker.IsEnabled;
            }
            set
            {
                _picker.IsEnabled = value;
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Picker (Dialog)";
            }
        }

        public ItemPickerDialogInput()
            : base()
        {
            Construct(null, new List<string>());
        }

        public ItemPickerDialogInput(string labelText, string tipText, List<string> items)
            : base(labelText)
        {
            Construct(tipText, items);
        }

        public ItemPickerDialogInput(string labelText, string name, string tipText, List<string> items)
            : base(labelText, name)
        {
            Construct(tipText, items);
        }

        private void Construct(string tipText, List<string> items)
        {
            _tipText = string.IsNullOrWhiteSpace(tipText) ? "Make selection here." : tipText;
            _items = items;
            _allowClearSelection = true;
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null && _items.Count > 0)
            {
                _picker = new Picker
                {
                    Title = _tipText,
                    HorizontalOptions = LayoutOptions.FillAndExpand

                    // set the style ID on the view so that we can retrieve it when UI testing
#if ENABLE_TEST_CLOUD
                    , StyleId = Name
#endif
                };

                if (_allowClearSelection)
                {
                    _picker.Items.Add("[Clear Selection]");
                }

                foreach (string item in RandomizeItemOrder ? _items.OrderBy(item => Guid.NewGuid()).ToList() : _items)
                {
                    _picker.Items.Add(item);
                }

                if(IncludeOtherOption && !string.IsNullOrWhiteSpace(OtherOptionText))
                {
                    _picker.Items.Add(OtherOptionText);
                }

                _picker.SelectedIndexChanged += (o, e) =>
                {
                    if (Value == null)
                    {
                        Complete = false;
                    }
                    else if (Value.ToString() == "[Clear Selection]")
                    {
                        _picker.SelectedIndex = -1;
                    }
                    else
                    {
                        Complete = true;
                    }
                };

                _label = CreateLabel(index);

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children = { _label, _picker }
                });
            }
            else
            {
                // if the view was already initialized, just update the label since the index might have changed.
                _label.Text = GetLabelText(index);

                // if the view is not enabled, there should be no tip text since the user can't do anything with the picker.
                if (!Enabled)
                {
                    _picker.Title = "";
                }
            }

            return base.GetView(index);
        }

        public override string ToString()
        {
            return base.ToString() + " -- " + (_items.Count + (IncludeOtherOption ? 1 : 0)) + " Items";
        }
    }
}