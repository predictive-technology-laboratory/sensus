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

        public override View View
        {
            get
            {
                if (base.View == null)
                {
                    _entry = new Entry
                    {
                        Keyboard = Keyboard.Default,
                        HorizontalOptions = LayoutOptions.FillAndExpand

                        // set the style ID on the view so that we can retrieve it when unit testing
                        #if UNIT_TESTING
                        , StyleId = Name
                        #endif
                    };

                    _entry.TextChanged += (o, e) => Complete = Value != null;

                    base.View = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { CreateLabel(), _entry }
                    };
                }

                return base.View;
            }
        }

        public override object Value
        {
            get
            {
                return _entry == null ? null : _entry.Text;
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

        public TextInput(string labelText)
            : base(labelText)
        {
        }

        public TextInput(string name, string labelText)
            : base(name, labelText)
        {            
        }
    }
}