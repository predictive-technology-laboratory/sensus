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
    public class NumberEntryInput : Input
    {
        private Entry _entry;
        private Label _label;
        private bool _hasFocused;

        public override object Value
        {
            get
            {
                double value;
                if (_entry == null || !_hasFocused || !double.TryParse(_entry.Text, out value))
                    return null;
                else
                    return value;
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
                return "Number Entry";
            }
        }

        public NumberEntryInput()
        {
        }

        public NumberEntryInput(string labelText)
            : base(labelText)
        {
        }

        public NumberEntryInput(string name, string labelText)
            : base(name, labelText)
        {            
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                _entry = new Entry
                {
                    Text = "Provide response here.",
                    FontSize = 20,
                    Keyboard = Keyboard.Numeric,
                    HorizontalOptions = LayoutOptions.FillAndExpand

                    // set the style ID on the view so that we can retrieve it when unit testing
                    #if UNIT_TESTING
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
                _label.Text = GetLabelText(index);

            return base.GetView(index);
        }
    }
}