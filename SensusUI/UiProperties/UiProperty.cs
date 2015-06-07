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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace SensusUI.UiProperties
{
    /// <summary>
    /// Attribute used to declare that a property should be rendered within the UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class UiProperty : Attribute
    {
        /// <summary>
        /// Gets a list of StackLayout objects associated with properties in an object that have been 
        /// decorated with a UiProperty attribute.
        /// </summary>
        /// <returns>The property stacks.</returns>
        /// <param name="o">Object to get StackLayouts for.</param>
        public static List<StackLayout> GetPropertyStacks(object o)
        {
            List<StackLayout> propertyStacks = new List<StackLayout>();

            List<Tuple<PropertyInfo, UiProperty>> propertyUiElements = o.GetType()
                                                                        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                                        .Select(p => new Tuple<PropertyInfo, UiProperty>(p, Attribute.GetCustomAttribute(p, typeof(UiProperty), true) as UiProperty))
                                                                        .Where(pp => pp.Item2 != null)
                                                                        .OrderBy(pp => pp.Item2._order).ToList();

            foreach (Tuple<PropertyInfo, UiProperty> propertyUiElement in propertyUiElements)
            {
                UiProperty uiElement = propertyUiElement.Item2;

                Label parameterNameLabel = new Label
                {
                    Text = uiElement.LabelText ?? propertyUiElement.Item1.Name + ":",
                    HorizontalOptions = LayoutOptions.Start,
                    FontSize = 20
                };

                bool addParameterValueLabel = false;

                View view = null;
                BindableProperty bindingProperty = null;
                IValueConverter converter = null;
                if (uiElement is OnOffUiProperty)
                {
                    view = new Switch();
                    bindingProperty = Switch.IsToggledProperty;
                }
                else if (uiElement is DisplayYesNoUiProperty)
                {
                    view = new Label
                    {
                        FontSize = 20
                    };

                    bindingProperty = Label.TextProperty;
                    converter = new DisplayYesNoUiProperty.ValueConverter();
                    uiElement.Editable = true;  // just makes the label text non-dimmed. a label's text is never editable.
                }
                else if (uiElement is DisplayStringUiProperty)
                {
                    view = new Label
                    {
                        FontSize = 20
                    };

                    bindingProperty = Label.TextProperty;
                    converter = new DisplayStringUiProperty.ValueConverter();
                    uiElement.Editable = true;  // just makes the label text non-dimmed. a label's text is never editable.
                }
                else if (uiElement is EntryIntegerUiProperty)
                {
                    view = new Entry
                    {
                        Keyboard = Keyboard.Numeric,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    bindingProperty = Entry.TextProperty;
                    converter = new EntryIntegerUiProperty.ValueConverter();
                }
                else if (uiElement is EntryFloatUiProperty)
                {
                    view = new Entry
                    {
                        Keyboard = Keyboard.Numeric,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    bindingProperty = Entry.TextProperty;
                    converter = new EntryFloatUiProperty.ValueConverter();
                }
                else if (uiElement is IncrementalIntegerUiProperty)
                {
                    IncrementalIntegerUiProperty p = uiElement as IncrementalIntegerUiProperty;
                    view = new Stepper
                    {
                        Minimum = p.Minimum,
                        Maximum = p.Maximum,
                        Increment = p.Increment
                    };

                    bindingProperty = Stepper.ValueProperty;
                    addParameterValueLabel = true;
                }
                else if (uiElement is EntryStringUiProperty)
                {
                    view = new Entry
                    {
                        Keyboard = Keyboard.Default,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    bindingProperty = Entry.TextProperty;
                }
                else if (uiElement is ListUiProperty)
                {
                    Picker picker = new Picker
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    List<object> items = (uiElement as ListUiProperty).Items.ToList();
                    foreach (object item in items)
                        picker.Items.Add(item.ToString());

                    picker.SelectedIndex = items.IndexOf(propertyUiElement.Item1.GetValue(o));

                    picker.SelectedIndexChanged += (oo, ee) =>
                        {
                            if (picker.SelectedIndex >= 0)
                                propertyUiElement.Item1.SetValue(o, items[picker.SelectedIndex]);
                        };

                    view = picker;
                }

                if (view != null)
                {
                    StackLayout stack = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    stack.Children.Add(parameterNameLabel);

                    if (addParameterValueLabel)
                    {
                        Label parameterValueLabel = new Label
                        {
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            FontSize = 20
                        };
                        parameterValueLabel.BindingContext = o;
                        parameterValueLabel.SetBinding(Label.TextProperty, propertyUiElement.Item1.Name);

                        stack.Children.Add(parameterValueLabel);
                    }

                    view.IsEnabled = uiElement.Editable;

                    if (bindingProperty != null)
                    {
                        view.BindingContext = o;
                        view.SetBinding(bindingProperty, new Binding(propertyUiElement.Item1.Name, converter: converter));
                    }

                    stack.Children.Add(view);

                    propertyStacks.Add(stack);
                }
            }

            return propertyStacks;
        }

        private string _labelText;
        private bool _editable;
        private int _order;

        public string LabelText
        {
            get { return _labelText; }
            set { _labelText = value; }
        }

        public bool Editable
        {
            get { return _editable; }
            set { _editable = value; }
        }

        protected UiProperty(string labelText, bool editable, int order)
        {
            _labelText = labelText;
            _editable = editable;
            _order = order;
        }
    }
}
