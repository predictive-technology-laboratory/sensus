//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
        [EntryStringUiProperty("Tip Text:", true, 10, false)]
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
        [EditableListUiProperty(null, true, 11, true)]
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
        [EntryStringUiProperty("Define Variable:", true, 13, false)]
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
            if (_items.Count == 0)
            {
                return null;
            }

            if (base.GetView(index) == null)
            {
                _picker = new Picker
                {
                    Title = _tipText,
                    HorizontalOptions = LayoutOptions.FillAndExpand

                    // set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
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
