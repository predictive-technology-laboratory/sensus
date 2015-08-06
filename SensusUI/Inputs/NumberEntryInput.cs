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

        public override bool Complete
        {
            get
            {
                double value;
                return _entry != null && double.TryParse(_entry.Text, out value);
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

        public override View CreateView(out Func<object> valueRetriever)
        {
            _entry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            valueRetriever = new Func<object>(() =>
                {
                    double value;
                    if (double.TryParse(_entry.Text, out value))
                        return value;
                    else
                        return null;
                });

            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { Label, _entry }
            };
        }
    }
}