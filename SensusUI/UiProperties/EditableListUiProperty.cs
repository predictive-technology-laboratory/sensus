﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace SensusUI.UiProperties
{
    public class EditableListUiProperty : UiProperty
    {
        public class ValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == null)
                    return "";

                StringBuilder text = new StringBuilder();
                foreach (string s in value as IEnumerable<string>)
                    text.AppendLine(s);

                return text.ToString();
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                try
                {
                    return value.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                catch (Exception)
                {
                    return new List<string>();
                }
            }
        }

        public EditableListUiProperty(string labelText, bool editable, int order)
            : base(labelText, editable, order)
        {
        }
    }
}