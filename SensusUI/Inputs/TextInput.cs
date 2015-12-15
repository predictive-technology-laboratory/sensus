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

namespace SensusUI.Inputs
{
    public class TextInput : Input
    {
        private Entry _entry;
        private Keyboard _keyboard;
        private Label _label;

        public override object Value
        {
            get
            {
                return _entry == null || string.IsNullOrWhiteSpace(_entry.Text) ? null : _entry.Text;
            }
        }

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
                return "Text Entry";
            }
        }

        public TextInput()
        {
        }

        public TextInput(string labelText, Keyboard keyboard)
            : base(labelText)
        {
            _keyboard = keyboard;
        }

        public TextInput(string name, string labelText, Keyboard keyboard)
            : base(name, labelText)
        {            
            _keyboard = keyboard;
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
                    HorizontalOptions = LayoutOptions.FillAndExpand

                    // set the style ID on the view so that we can retrieve it when unit testing
                    #if UNIT_TESTING
                    , StyleId = Name
                    #endif
                };

                Color defaultTextColor = _entry.TextColor;
                _entry.TextColor = Color.Gray;
                bool firstFocus = true;
                _entry.Focused += (o, e) =>
                {
                    if (firstFocus)
                    {
                        _entry.Text = "";
                        _entry.TextColor = defaultTextColor;
                        firstFocus = false;
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
                _label.Text = GetLabelText(index);

            return base.GetView(index);
        }
    }
}