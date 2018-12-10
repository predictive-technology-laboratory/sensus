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

        public override bool Store => false;

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
