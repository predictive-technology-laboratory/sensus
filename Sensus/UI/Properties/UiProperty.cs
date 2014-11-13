using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace Sensus.UI.Properties
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class UiProperty : Attribute
    {
        public static List<StackLayout> GetPropertyStacks(object o)
        {
            List<StackLayout> propertyStacks = new List<StackLayout>();

            foreach (PropertyInfo property in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(p => p.Name))
            {
                UiProperty probeParameterAttribute = Attribute.GetCustomAttribute(property, typeof(UiProperty), true) as UiProperty;
                if (probeParameterAttribute != null)
                {
                    Label parameterNameLabel = new Label
                    {
                        Text = probeParameterAttribute.LabelText,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Font = Font.SystemFontOfSize(20)
                    };

                    Label parameterValueLabel = new Label
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Font = Font.SystemFontOfSize(20)
                    };
                    parameterValueLabel.BindingContext = o;
                    parameterValueLabel.SetBinding(Label.TextProperty, property.Name);

                    View view = null;
                    BindableProperty bindingProperty = null;
                    IValueConverter converter = null;
                    if (probeParameterAttribute is BooleanUiProperty)
                    {
                        view = new Switch();
                        bindingProperty = Switch.IsToggledProperty;
                    }
                    else if (probeParameterAttribute is EntryIntegerUiProperty)
                    {
                        view = new Entry
                        {
                            Keyboard = Keyboard.Numeric
                        };
                        bindingProperty = Entry.TextProperty;
                        converter = new EntryIntegerUiProperty.ValueConverter();
                    }
                    else if (probeParameterAttribute is IncrementalIntegerUiProperty)
                    {
                        IncrementalIntegerUiProperty p = probeParameterAttribute as IncrementalIntegerUiProperty;
                        view = new Stepper
                        {
                            Minimum = p.Minimum,
                            Maximum = p.Maximum,
                            Increment = p.Increment
                        };
                        bindingProperty = Stepper.ValueProperty;
                    }
                    else if (probeParameterAttribute is StringUiProperty)
                    {
                        view = new Entry
                        {
                            Keyboard = Keyboard.Default
                        };
                        bindingProperty = Entry.TextProperty;
                    }

                    if (view != null)
                    {
                        view.IsEnabled = probeParameterAttribute.Editable;
                        view.BindingContext = o;
                        view.SetBinding(bindingProperty, new Binding(property.Name, converter: converter));

                        propertyStacks.Add(new StackLayout
                        {
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            Orientation = StackOrientation.Horizontal,
                            Children = { parameterNameLabel, parameterValueLabel, view }
                        });
                    }
                }
            }

            return propertyStacks;
        }

        private string _labelText;
        private bool _editable;

        public string LabelText
        {
            get { return _labelText; }
        }

        public bool Editable
        {
            get { return _editable; }
        }

        protected UiProperty(string labelText, bool editable)
        {
            _labelText = labelText;
            _editable = editable;
        }
    }
}
