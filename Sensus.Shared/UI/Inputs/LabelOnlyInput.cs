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

namespace Sensus.UI.Inputs
{
    public class LabelOnlyInput : Input
    {
        public override object Value
        {
            get
            {
                return null;
            }
        }

        [JsonIgnore]
        public override bool Enabled
        {
            get
            {
                return true;
            }
            set
            {
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

        public LabelOnlyInput(string labelText, int labelFontSize)
            : base(labelText, labelFontSize)
        {
            Construct(true);
        }

        public LabelOnlyInput(string labelText, bool complete)
            : base(labelText)
        {
            Construct(complete);
        }

        public LabelOnlyInput(string labelText, string name)
            : base(labelText, name)
        {
            Construct(true);
        }

        private void Construct(bool complete)
        {
            // don't store completion records. label-only inputs are complete by definition and repeated deserialization will accumulate
            // completion records that don't have meaning:  https://github.com/predictive-technology-laboratory/sensus/issues/126
            // set this before anything else, particularly Complete as that triggers a record storage.
            StoreCompletionRecords = false;

            Complete = complete;
            Required = false;
            DisplayNumber = false;
            NeedsToBeStored = false;
            Frame = false;
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                base.SetView(CreateLabel(-1));
            }

            return base.GetView(index);
        }
    }
}