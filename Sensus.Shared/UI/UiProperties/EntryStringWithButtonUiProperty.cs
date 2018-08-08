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
using System.Reflection;
using System.Linq.Expressions;

namespace Sensus.UI.UiProperties
{
    /// <summary>
    /// Decorated members should be rendered as editable strings with a trailing button.
    /// </summary>
    public class EntryStringWithButtonUiProperty : UiProperty
    {
        private string _buttonText;
        private string _delegateName;
        private Type _objectType;
        public EntryStringWithButtonUiProperty(string labelText, string buttonText, string delegateName, Type objectType, bool editable, int order, bool required)
            : base(labelText, editable, order, required)
        {
            _buttonText = buttonText;
            _delegateName = delegateName;
            _objectType = objectType;
        }

        public override View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter)
        {
            bindingProperty = Entry.TextProperty;
            converter = null;
            EventHandler _eventHandler = (s, e) =>
            {
                _objectType.GetMethod(_delegateName).Invoke(o, new[] {s, e });
            };
            Entry entry =  new Entry
            {
                Keyboard = Keyboard.Default,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            Button button = new Button
            {
                Text = _buttonText,
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            button.Clicked += _eventHandler;

            StackLayout stack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 10
            };
            stack.Children.Add(entry);
            stack.Children.Add(button);

            BindableOverride = entry;

            return stack;
        }
    }
}