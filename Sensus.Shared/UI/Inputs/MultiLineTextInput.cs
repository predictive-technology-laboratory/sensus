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
    public class MultiLineTextInput : Input
    {
        private Editor _editor;
        private Keyboard _keyboard;
        private Label _label;
        private bool _hasFocused;
        private int _height;

        [EntryIntegerUiProperty(null, true, 5)]
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (value < 100)
                {
                    value = 100;
                }

                _height = value;
            }
        }

        public override object Value
        {
            get
            {
                return _editor == null || !_hasFocused || string.IsNullOrWhiteSpace(_editor.Text) ? null : _editor.Text;
            }
        }

        [JsonIgnore]
        public override bool Enabled
        {
            get
            {
                return _editor.IsEnabled;
            }
            set
            {
                _editor.IsEnabled = value;
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Multi-Line Text Entry";
            }
        }

        public MultiLineTextInput()
        {
        }

        public MultiLineTextInput(string labelText, Keyboard keyboard)
            : base(labelText)
        {
            Construct(keyboard);
        }

        public MultiLineTextInput(string name, string labelText, Keyboard keyboard)
            : base(name, labelText)
        {
            Construct(keyboard);
        }

        private void Construct(Keyboard keyboard)
        {
            _keyboard = keyboard;
            _height = 100;
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                _editor = new Editor
                {
                    Text = "Provide response here.",
                    FontSize = 20,
                    Keyboard = _keyboard,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = _height

                    // set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
                    , StyleId = Name
#endif
                };

                Color defaultTextColor = _editor.TextColor;
                _editor.TextColor = Color.Gray;
                _hasFocused = false;
                _editor.Focused += (o, e) =>
                {
                    if (!_hasFocused)
                    {
                        _editor.Text = "";
                        _editor.TextColor = defaultTextColor;
                        _hasFocused = true;
                    }
                };

                _editor.TextChanged += (o, e) =>
                {
                    Complete = Value != null;
                };

                _label = CreateLabel(index);

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children = { _label, _editor }
                });
            }
            else
            {
                // if the view was already initialized, just update the label since the index might have changed.
                _label.Text = GetLabelText(index);

                // if the view is not enabled, there should be no tip text since the user can't do anything with the entry.
                if (!Enabled && !_hasFocused)
                {
                    _editor.Text = "";
                }
            }

            return base.GetView(index);
        }
    }
}