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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Sensus.UI.UiProperties
{
    /// <summary>
    /// Decorated members should be rendered as lists.
    /// </summary>
    public class ListUiProperty : UiProperty
    {
        private List<object> _items;

        public ListUiProperty(string labelText, bool editable, int order, object[] items)
            : base(labelText, editable, order)
        {
            if (items == null)
                items = new object[0];

            _items = items.ToList();
        }

        public override View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter)
        {
            bindingProperty = null;
            converter = null;

            Picker picker = new Picker
            {
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            foreach (object item in _items)
            {
                picker.Items.Add(item.ToString());
            }

            picker.SelectedIndex = _items.IndexOf(property.GetValue(o));

            picker.SelectedIndexChanged += (oo, ee) =>
            {
                if (picker.SelectedIndex >= 0)
                {
                    property.SetValue(o, _items[picker.SelectedIndex]);
                }
            };

            return picker;
        }
    }
}