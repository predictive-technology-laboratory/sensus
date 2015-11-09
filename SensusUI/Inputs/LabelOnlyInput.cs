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
    public class LabelOnlyInput : Input
    {
        public override View View
        {
            get
            {
                if (base.View == null)
                {
                    // set the style ID on the view so that we can retrieve it when unit testing
                    #if UNIT_TESTING
                    Label.StyleId = Name;
                    #endif
                        
                    base.View = Label;
                }
                
                return base.View;
            }
        }

        public override object Value
        {
            get
            {
                return null;
            }
        }

        public override bool Enabled
        {
            get
            {
                return Label.IsEnabled;
            }
            set
            {
                Label.IsEnabled = value;
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Read-only Text";
            }
        }

        public LabelOnlyInput()
        {
            Construct(true);
        }

        public LabelOnlyInput(string labelText)
            : base(labelText)
        {
            Construct(true);
        }

        public LabelOnlyInput(string labelText, bool complete)
            : base(labelText)
        {
            Construct(complete);
        }

        public LabelOnlyInput(string name, string labelText)
            : base(name, labelText)
        {
            Construct(true);
        }

        public LabelOnlyInput(string name, string labelText, bool complete)
            : base(name, labelText)
        {
            Construct(complete);
        }

        private void Construct(bool complete)
        {
            Complete = complete;
            ShouldBeStored = false;
        }
    }
}