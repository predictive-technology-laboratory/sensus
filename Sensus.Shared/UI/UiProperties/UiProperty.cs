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
using Xamarin.Essentials;

namespace Sensus.UI.UiProperties
{
	/// <summary>
	/// Attribute used to declare that a property should be rendered within the UI.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public abstract class UiProperty : Attribute
	{
		private const string REQUIRED_MARK = "*";

		/// <summary>
		/// Gets the <see cref="UiProperty"/> attribute associated with a property.
		/// </summary>
		/// <returns>The user interface property attribute.</returns>
		/// <param name="property">Property.</param>
		private static UiProperty GetUiPropertyAttribute(PropertyInfo property)
		{
			if (property == null)
			{
				return null;
			}

			// For some reason, PropertyInfo.GetCustomAttributes doesn't return the UiProperty attribute placed on abstract 
			// properties that are overridden, so we have to navigate the inheritance tree to search for attributes.
			// bzd3y: This is probably due to Inherited not being set to true in the [AttributeUsage] attribute above. The
			// code below would probably be unnecessary if that was done. I'm not changing anything, but this can be a
			// reminder of a possible future change.
			// 

			UiProperty[] attributes = property.GetCustomAttributes<UiProperty>().ToArray();

			if (attributes.OfType<HiddenUiProperty>().FirstOrDefault() is HiddenUiProperty hiddenAttribute)
			{
				return hiddenAttribute;
			}

			UiProperty attribute = attributes.FirstOrDefault();

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

		public static bool HasUiProperties(object o)
		{
			return o.GetType()
				 .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				 .Any(x => GetUiPropertyAttribute(x) != null);
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
				 .Where(p => p.Item2 != null && (p.Item2 is HiddenUiProperty == false))
				 .OrderBy(p => p.Item2._order).ToList();

			List<StackLayout> propertyStacks = new List<StackLayout>();

			foreach (Tuple<PropertyInfo, UiProperty> propertyUiElement in propertyUiElements)
			{
				PropertyInfo property = propertyUiElement.Item1;
				UiProperty uiElement = propertyUiElement.Item2;

				Label propertyLabel = new Label
				{
					Text = uiElement.LabelText ?? property.Name + ":" + (uiElement._required ? REQUIRED_MARK : ""),
					FontSize = 20
				};

				TapGestureRecognizer labelTapRecognizer = new TapGestureRecognizer()
				{
					NumberOfTapsRequired = 1
				};

				labelTapRecognizer.Tapped += async (sender, e) => 
				{
					// https://predictive-technology-laboratory.github.io/sensus/api/Sensus.Probes.PollingProbe.html#Sensus_Probes_PollingProbe_PollingSleepDurationMS
					await Launcher.OpenAsync(new Uri($"https://predictive-technology-laboratory.github.io/sensus/api/{property.DeclaringType}.html#{property.DeclaringType.ToString().Replace('.', '_') + "_" + property.Name}"));
				};

				propertyLabel.GestureRecognizers.Add(labelTapRecognizer);

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
		private bool _required;

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

		protected UiProperty(string labelText, bool editable, int order, bool required)
		{
			_labelText = string.IsNullOrWhiteSpace(labelText) ? null : labelText + (required ? REQUIRED_MARK : "");
			_editable = editable;
			_order = order;
			_required = required;
		}

		public abstract View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter);
	}
}