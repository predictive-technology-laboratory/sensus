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

using SensusService.Exceptions;
using System;
using Xamarin.Forms;
using System.Reflection;

namespace SensusUI.UiProperties
{
    /// <summary>
    /// Decorated members should be rendered as display-only text.
    /// </summary>
    public class DisplayStringUiProperty : UiProperty
    {
        public class ValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return "";
                
                return value.ToString();
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
                return null;
            }
        }

        public DisplayStringUiProperty(string labelText, int order)
            : base(labelText, true, order)
        {
        }

        public override View GetView(PropertyInfo property, object o, out BindableProperty targetProperty, out IValueConverter converter)
        {
            targetProperty = Label.TextProperty;
            converter = new ValueConverter();

            return new Label
            {
                FontSize = 20
            };
        }
    }
}
