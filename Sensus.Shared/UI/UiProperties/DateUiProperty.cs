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
using System.Reflection;

namespace Sensus.UI.UiProperties
{
    /// <summary>
    /// Decorated members should be rendered as editable dates.
    /// </summary>
    public class DateUiProperty : UiProperty
    {
        public DateUiProperty(string labelText, bool editable, int order, bool required)
            : base(labelText, editable, order, required)
        {
        }

        public override View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter)
        {
            bindingProperty = DatePicker.DateProperty;
            converter = null;

            return new DatePicker
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                MinimumDate = DateTime.Now.Date
            };
        }
    }
}