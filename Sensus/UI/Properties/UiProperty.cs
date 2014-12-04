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
                        HorizontalOptions = LayoutOptions.Start,
                        Font = Font.SystemFontOfSize(20)
                    };

                    bool addParameterValueLabel = false;

                    View view = null;
                    BindableProperty bindingProperty = null;
                    IValueConverter converter = null;
                    if (probeParameterAttribute is OnOffUiProperty)
                    {
                        view = new Switch();
                        bindingProperty = Switch.IsToggledProperty;
                    }
                    else if(probeParameterAttribute is DisplayYesNoUiProperty)
                    {
                        view = new Label
                        {
                            Font = Font.SystemFontOfSize(20)
                        };

                        bindingProperty = Label.TextProperty;
                        converter = new DisplayYesNoUiProperty.ValueConverter();
                        probeParameterAttribute.Editable = true;  // just makes the label text non-dimmed. a label's text is never editable.
                    }
                    else if(probeParameterAttribute is DisplayStringUiProperty)
                    {
                        view = new Label
                        {
                            Font = Font.SystemFontOfSize(20)
                        };

                        bindingProperty = Label.TextProperty;
                        converter = new DisplayStringUiProperty.ValueConverter();
                        probeParameterAttribute.Editable = true;  // just makes the label text non-dimmed. a label's text is never editable.
                    }
                    else if (probeParameterAttribute is EntryIntegerUiProperty)
                    {
                        view = new Entry
                        {
                            Keyboard = Keyboard.Numeric,
                            HorizontalOptions = LayoutOptions.FillAndExpand
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

                        addParameterValueLabel = true;
                    }
                    else if (probeParameterAttribute is EntryStringUiProperty)
                    {
                        view = new Entry
                        {
                            Keyboard = Keyboard.Default,                           
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        };
                        bindingProperty = Entry.TextProperty;
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
                                Font = Font.SystemFontOfSize(20)
                            };
                            parameterValueLabel.BindingContext = o;
                            parameterValueLabel.SetBinding(Label.TextProperty, property.Name);

                            stack.Children.Add(parameterValueLabel);
                        }

                        view.IsEnabled = probeParameterAttribute.Editable;
                        view.BindingContext = o;
                        view.SetBinding(bindingProperty, new Binding(property.Name, converter: converter));

                        stack.Children.Add(view);

                        propertyStacks.Add(stack);
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
            set { _labelText = value; }
        }

        public bool Editable
        {
            get { return _editable; }
            set { _editable = value; }
        }

        protected UiProperty(string labelText, bool editable)
        {
            _labelText = labelText;
            _editable = editable;
        }
    }
}
