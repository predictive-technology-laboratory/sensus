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
using SensusUI.UiProperties;
using System.Collections.Generic;

namespace SensusUI.Inputs
{
    public class ItemPickerInput : Input
    {
        private string _tipText;
        private List<string> _items;
        private bool _allowClearSelection;
        private Picker _picker;

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

        [EditableListUiProperty(null, true, 11)]
        public List<string> Items
        {
            get
            {
                return _items;
            }
            // need set method so auto-binding can set the list via the EditableListUiProperty
            set
            {
                _items = value;
            }
        }

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

        public override View View
        {
            get
            {
                if (base.View == null && _items.Count > 0)
                {
                    _picker = new Picker
                    {
                        Title = _tipText,
                        HorizontalOptions = LayoutOptions.FillAndExpand

                        // set the style ID on the view so that we can retrieve it when unit testing
                        #if UNIT_TESTING
                        , StyleId = Name
                        #endif
                    };

                    if (_allowClearSelection)
                        _picker.Items.Add("[Clear Selection]");
                    
                    foreach (string item in _items)
                        _picker.Items.Add(item);

                    _picker.SelectedIndexChanged += (o, e) =>
                    {
                        if (Value == null)
                            Complete = false;
                        else if (Value.ToString() == "[Clear Selection]")
                            _picker.SelectedIndex = -1;
                        else
                            Complete = true;
                    };
                    
                    base.View = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { CreateLabel(), _picker }
                    };
                }

                return base.View;
            }
        }

        public override object Value
        {
            get
            {
                return _picker == null || _picker.SelectedIndex < 0 ? null : _picker.Items[_picker.SelectedIndex];
            }
        }

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
                return "Picker";
            }
        }

        public ItemPickerInput()
        {
            Construct("Please Make Selection", new List<string>());
        }

        public ItemPickerInput(string labelText, string tipText, List<string> items)
            : base(labelText)
        {
            Construct(tipText, items);
        }

        public ItemPickerInput(string name, string labelText, string tipText, List<string> items)
            : base(name, labelText)
        {
            Construct(tipText, items);      
        }

        private void Construct(string tipText, List<string> items)
        {
            _tipText = tipText;
            _items = items;
            _allowClearSelection = true;
        }

        public override string ToString()
        {
            return base.ToString() + " -- " + _items.Count + " Items";
        }
    }
}