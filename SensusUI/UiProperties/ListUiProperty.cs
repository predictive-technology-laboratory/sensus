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

namespace SensusUI.UiProperties
{
    /// <summary>
    /// Decorated members should be rendered as lists.
    /// </summary>
    public class ListUiProperty : UiProperty
    {
        private object[] _items;

        public object[] Items
        {
            get { return _items; }
        }

        public ListUiProperty(string labelText, bool editable, int order, object[] items)
            : base(labelText, editable, order)
        {
            _items = items;

            if(_items == null)
                _items = new object[0];
        }       
    }
}
