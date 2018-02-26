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
        [EntryStringUiProperty("Text for \"Other\" Option:", true, 14)]
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

        public ItemPickerInput(string name, string labelText)
            : base(name, labelText)
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