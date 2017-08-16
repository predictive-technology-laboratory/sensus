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

using Xamarin.Forms;
using Newtonsoft.Json;
using Sensus.UI.UiProperties;

namespace Sensus.UI.Inputs
{
    public class SingleLineTextInput : Input, IVariableDefiningInput
    {
        private Entry _entry;
        private Keyboard _keyboard;
        private Label _label;
        private bool _hasFocused;
        private bool _masked;
        private string _definedVariable;

        [OnOffUiProperty(null, true, 1)]
        public bool Masked
        {
            get
            {
                return _masked;
            }
            set
            {
                _masked = value;
            }
        }

        [EntryStringUiProperty("Define Variable:", true, 2)]
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
                return _entry == null || !_hasFocused || string.IsNullOrWhiteSpace(_entry.Text) ? null : _entry.Text;
            }
        }

        [JsonIgnore]
        public override bool Enabled
        {
            get
            {
                return _entry.IsEnabled;
            }
            set
            {
                _entry.IsEnabled = value;
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Single-Line Text Entry";
            }
        }

        public SingleLineTextInput()
        {
        }

        public SingleLineTextInput(string labelText, Keyboard keyboard, bool masked = false)
            : base(labelText)
        {
            _keyboard = keyboard;
            _masked = masked;
        }

        public SingleLineTextInput(string name, string labelText, Keyboard keyboard, bool masked = false)
            : base(name, labelText)
        {
            _keyboard = keyboard;
            _masked = masked;
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                _entry = new Entry
                {
                    Text = "Provide response here.",
                    FontSize = 20,
                    Keyboard = _keyboard,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    IsPassword = _masked

                    // set the style ID on the view so that we can retrieve it when UI testing
#if ENABLE_TEST_CLOUD
                    , StyleId = Name
#endif
                };

                Color defaultTextColor = _entry.TextColor;
                _entry.TextColor = Color.Gray;
                _hasFocused = false;
                _entry.Focused += (o, e) =>
                {
                    if (!_hasFocused)
                    {
                        _entry.Text = "";
                        _entry.TextColor = defaultTextColor;
                        _hasFocused = true;
                    }
                };

                _entry.TextChanged += (o, e) =>
                {
                    Complete = Value != null;
                };

                _label = CreateLabel(index);

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children = { _label, _entry }
                });
            }
            else
            {
                // if the view was already initialized, just update the label since the index might have changed.
                _label.Text = GetLabelText(index);

                // if the view is not enabled, there should be no tip text since the user can't do anything with the entry.
                if (!Enabled && !_hasFocused)
                {
                    _entry.Text = "";
                }
            }

            return base.GetView(index);
        }
    }
}