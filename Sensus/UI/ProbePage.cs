using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using Xamarin.Forms;
using Sensus.Probes.Parameters;

namespace Sensus.UI
{
    /// <summary>
    /// Displays properties for a single probe.
    /// </summary>
    public class ProbePage : ContentPage
    {
        public ProbePage(Probe probe)
        {
            BindingContext = probe;

            SetBinding(TitleProperty, new Binding("Name"));

            List<StackLayout> stacks = new List<StackLayout>();

            #region name
            Label nameLabel = new Label
            {
                Text = "Name:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Entry nameEntry = new Entry();
            nameEntry.BindingContext = probe;
            nameEntry.SetBinding(Entry.TextProperty, "Name");

            stacks.Add(new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { nameLabel, nameEntry }
            });
            #endregion

            #region status
            Label statusLabel = new Label
            {
                Text = "Status:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Switch statusSwitch = new Switch();
            statusSwitch.BindingContext = probe;
            statusSwitch.SetBinding(Switch.IsToggledProperty, "Enabled");

            stacks.Add(new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { statusLabel, statusSwitch }
            });
            #endregion

            #region parameters
            foreach (PropertyInfo property in probe.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(p => p.Name))
            {
                ProbeParameter probeParameterAttribute = Attribute.GetCustomAttribute(property, typeof(ProbeParameter), true) as ProbeParameter;
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
                    parameterValueLabel.BindingContext = probe;
                    parameterValueLabel.SetBinding(Label.TextProperty, property.Name);

                    View view = null;
                    BindableProperty bindingProperty = null;
                    IValueConverter converter = null;
                    if (probeParameterAttribute is BooleanProbeParameter)
                    {
                        view = new Switch();
                        bindingProperty = Switch.IsToggledProperty;
                    }
                    else if (probeParameterAttribute is EntryIntegerProbeParameter)
                    {
                        view = new Entry
                        {
                            Keyboard = Keyboard.Numeric
                        };
                        bindingProperty = Entry.TextProperty;
                        converter = new EntryIntegerProbeParameter.ValueConverter();
                    }
                    else if(probeParameterAttribute is IncrementalIntegerProbeParameter)
                    {
                        IncrementalIntegerProbeParameter p = probeParameterAttribute as IncrementalIntegerProbeParameter;
                        view = new Stepper
                        {
                            Minimum = p.Minimum,
                            Maximum = p.Maximum,
                            Increment = p.Increment
                        };
                        bindingProperty = Stepper.ValueProperty;
                    }
                    else if (probeParameterAttribute is StringProbeParameter)
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
                        view.BindingContext = probe;
                        view.SetBinding(bindingProperty, new Binding(property.Name, converter: converter));

                        stacks.Add(new StackLayout
                        {
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            Orientation = StackOrientation.Horizontal,
                            Children = { parameterNameLabel, parameterValueLabel, view }
                        });
                    }
                }
            }
            #endregion

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            foreach (StackLayout stack in stacks)
                (Content as StackLayout).Children.Add(stack);
        }
    }
}
