#region copyright
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
#endregion

using SensusService.Exceptions;
using System;
using Xamarin.Forms;

namespace SensusUI.UiProperties
{
    public class LoadTextFileUiProperty : UiProperty
    {
        public class ValueConverter : IValueConverter
        {
            private string _defaultValue;

            public ValueConverter(string defaultValue)
            {
                _defaultValue = defaultValue;
            }

            public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value == null ? _defaultValue : value;
            }

            public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        private string _buttonText;
        private string _prompt;

        public string ButtonText
        {
            get { return _buttonText; }
        }

        public string Prompt
        {
            get { return _prompt; }
        }

        public LoadTextFileUiProperty(string labelText, bool editable, int order, string buttonText, string prompt)
            : base(labelText, editable, order)
        {
            _buttonText = buttonText;
            _prompt = prompt;
        }
    }
}
