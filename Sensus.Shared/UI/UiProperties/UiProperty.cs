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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI.UiProperties
{
    /// <summary>
    /// Attribute used to declare that a property should be rendered within the UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class UiProperty : Attribute
    {
        /// <summary>
        /// Gets the UiProperty attribute associated with a property. For some reason, PropertyInfo.GetCustomAttributes doesn't return the 
        /// UiProperty attribute placed on abstract properties that are overridden.
        /// </summary>
        /// <returns>The user interface property attribute.</returns>
        /// <param name="property">Property.</param>
        public static UiProperty GetUiPropertyAttribute(PropertyInfo property)
        {
            if (property == null)
            {
                return null;
            }

            UiProperty attribute = property.GetCustomAttribute<UiProperty>();

            if (attribute == null)
            {
                Type parentType = property.ReflectedType.BaseType;

                if (parentType == null)
                {
                    return null;
                }
                else
                {
                    return GetUiPropertyAttribute(parentType.GetProperty(property.Name));
                }
            }
            else
            {
                return attribute;
            }
        }

        /// <summary>
        /// Gets a list of StackLayout objects associated with properties in an object that have been 
        /// decorated with a UiProperty attribute.
        /// </summary>
        /// <returns>The property stacks.</returns>
        /// <param name="o">Object to get StackLayouts for.</param>
        public static List<StackLayout> GetPropertyStacks(object o)
        {
            List<Tuple<PropertyInfo, UiProperty>> propertyUiElements =
                o.GetType()
                 .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 .Select(p => new Tuple<PropertyInfo, UiProperty>(p, GetUiPropertyAttribute(p)))
                 .Where(p => p.Item2 != null)
                 .OrderBy(p => p.Item2._order).ToList();

            List<StackLayout> propertyStacks = new List<StackLayout>();

            foreach (Tuple<PropertyInfo, UiProperty> propertyUiElement in propertyUiElements)
            {
                PropertyInfo property = propertyUiElement.Item1;
                UiProperty uiElement = propertyUiElement.Item2;

                Label propertyLabel = new Label
                {
                    Text = uiElement.LabelText ?? property.Name + ":",
                    FontSize = 20
                };

                BindableProperty targetProperty = null;
                IValueConverter converter = null;
                View propertyView = uiElement.GetView(property, o, out targetProperty, out converter);
                propertyView.IsEnabled = uiElement.Editable;

#if UI_TESTING
                // set style id so we can get the property value when UI testing
                propertyView.StyleId = propertyLabel.Text + " View";
#endif

                if (targetProperty != null)
                {
                    propertyView.BindingContext = o;
                    propertyView.SetBinding(targetProperty, new Binding(property.Name, converter: converter));
                }

                propertyStacks.Add(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children = { propertyLabel, propertyView }
                });
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

        public abstract View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter);
    }
}