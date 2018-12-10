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

using Sensus.UI.UiProperties;

namespace Sensus.UI.Inputs
{
    public abstract class ItemPickerInput : Input
    {
        private bool _randomizeItemOrder;

        /// <summary>
        /// Whether or not to randomize the order of items to choose from.
        /// </summary>
        /// <value><c>true</c> to randomize item order; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Randomize Item Order:", true, 12)]
        public bool RandomizeItemOrder
        {
            get
            {
                return _randomizeItemOrder;
            }
            set
            {
                _randomizeItemOrder = value;
            }
        }

        /// <summary>
        /// Whether or not to include an `Other` option in the list of items.
        /// </summary>
        /// <value><c>true</c> if include other option; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Include \"Other\" Option:", true, 13)]
        public bool IncludeOtherOption { get; set; }

        /// <summary>
        /// The text to show for the "Other" option in a multiple-choice list.
        /// </summary>
        /// <value>The other option text.</value>
        [EntryStringUiProperty("Text for \"Other\" Option:", true, 14, false)]
        public string OtherOptionText { get; set; }

        public ItemPickerInput()
        {
            Construct();
        }

        public ItemPickerInput(string labelText)
            : base(labelText)
        {
            Construct();
        }

        public ItemPickerInput(string labelText, string name)
            : base(labelText, name)
        {
            Construct();
        }

        private void Construct()
        {
            _randomizeItemOrder = false;
            IncludeOtherOption = false;
            OtherOptionText = "Other";
        }
    }
}
