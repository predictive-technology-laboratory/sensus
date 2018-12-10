//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Reflection;
using Sensus.Probes;
using Sensus.Probes.User.Scripts;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Allows the user to add a script trigger to a script runner.
    /// </summary>
    public class AddScriptTriggerPage : ContentPage
    {
        private ScriptRunner _scriptRunner;
        private Probe _selectedProbe;
        private PropertyInfo _selectedDatumProperty;
        private TriggerValueCondition _selectedCondition;
        private object _conditionValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddScriptTriggerPage"/> class.
        /// </summary>
        /// <param name="scriptRunner">Script runner to add trigger to.</param>
        public AddScriptTriggerPage(ScriptRunner scriptRunner)
        {
            _scriptRunner = scriptRunner;

            Title = "Add Trigger";

            Probe[] enabledProbes = _scriptRunner.Probe.Protocol.Probes.Where(p => p != _scriptRunner.Probe && p.Enabled).ToArray();

            if (!enabledProbes.Any())
            {
                Content = new Label { Text = "No enabled probes. Please enable them before creating triggers.", FontSize = 20 };

                return;
            }

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            Label probeLabel = new Label 
            { 
                Text = "Probe:", 
                FontSize = 20,
                VerticalTextAlignment = TextAlignment.Center
            };

            Picker probePicker = new Picker { Title = "Select Probe", HorizontalOptions = LayoutOptions.FillAndExpand };

            foreach (Probe enabledProbe in enabledProbes)
            {
                probePicker.Items.Add(enabledProbe.DisplayName);
            }

            contentLayout.Children.Add(new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { probeLabel, probePicker }
            });

            StackLayout triggerDefinitionLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.Start
            };

            contentLayout.Children.Add(triggerDefinitionLayout);

            bool allowChangeCalculation = false;
            Switch changeSwitch = new Switch();
            bool allowRegularExpression = false;
            Switch regexSwitch = new Switch();
            Switch fireRepeatedlySwitch = new Switch();
            TimePicker startTimePicker = new TimePicker { HorizontalOptions = LayoutOptions.FillAndExpand };
            TimePicker endTimePicker = new TimePicker { HorizontalOptions = LayoutOptions.FillAndExpand };

            probePicker.SelectedIndexChanged += (o, e) =>
            {
                _selectedProbe = null;
                _selectedDatumProperty = null;
                _conditionValue = null;

                triggerDefinitionLayout.Children.Clear();

                if (probePicker.SelectedIndex < 0)
                {
                    return;
                }

                _selectedProbe = enabledProbes[probePicker.SelectedIndex];

                PropertyInfo[] datumProperties = _selectedProbe.DatumType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttributes<ProbeTriggerProperty>().Any()).ToArray();

                if (datumProperties.Length == 0)
                {
                    return;
                }

                #region datum property picker
                Label datumPropertyLabel = new Label
                {
                    Text = "Property:",
                    FontSize = 20,
                    VerticalTextAlignment = TextAlignment.Center
                };

                Picker datumPropertyPicker = new Picker { Title = "Select Datum Property", HorizontalOptions = LayoutOptions.FillAndExpand };
                foreach (PropertyInfo datumProperty in datumProperties)
                {
                    ProbeTriggerProperty triggerProperty = datumProperty.GetCustomAttributes<ProbeTriggerProperty>().First();
                    datumPropertyPicker.Items.Add(triggerProperty.Name ?? datumProperty.Name);
                }

                triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { datumPropertyLabel, datumPropertyPicker }
                });
                #endregion

                #region condition picker (same for all datum types)
                Label conditionLabel = new Label
                {
                    Text = "Condition:",
                    FontSize = 20,
                    VerticalTextAlignment = TextAlignment.Center
                };

                Picker conditionPicker = new Picker { Title = "Select Condition", HorizontalOptions = LayoutOptions.FillAndExpand };
                TriggerValueCondition[] conditions = Enum.GetValues(typeof(TriggerValueCondition)) as TriggerValueCondition[];
                foreach (TriggerValueCondition condition in conditions)
                {
                    conditionPicker.Items.Add(condition.ToString());
                }

                conditionPicker.SelectedIndexChanged += (oo, ee) =>
                {
                    if (conditionPicker.SelectedIndex < 0)
                    {
                        return;
                    }

                    _selectedCondition = conditions[conditionPicker.SelectedIndex];
                };

                triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { conditionLabel, conditionPicker }
                });
                #endregion

                #region condition value for comparison, based on selected datum property -- includes change calculation (for double datum) and regex (for string datum)
                StackLayout conditionValueStack = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                triggerDefinitionLayout.Children.Add(conditionValueStack);

                datumPropertyPicker.SelectedIndexChanged += (oo, ee) =>
                {
                    _selectedDatumProperty = null;
                    _conditionValue = null;

                    conditionValueStack.Children.Clear();

                    if (datumPropertyPicker.SelectedIndex < 0)
                    {
                        return;
                    }

                    _selectedDatumProperty = datumProperties[datumPropertyPicker.SelectedIndex];

                    ProbeTriggerProperty datumTriggerAttribute = _selectedDatumProperty.GetCustomAttribute<ProbeTriggerProperty>();

                    View conditionValueStackView = null;

                    if (datumTriggerAttribute is ListProbeTriggerProperty)
                    {
                        Picker conditionValuePicker = new Picker { Title = "Select Condition Value", HorizontalOptions = LayoutOptions.FillAndExpand };
                        object[] items = (datumTriggerAttribute as ListProbeTriggerProperty).Items;
                        foreach (object item in items)
                        {
                            conditionValuePicker.Items.Add(item.ToString());
                        }

                        conditionValuePicker.SelectedIndexChanged += (ooo, eee) =>
                        {
                            if (conditionValuePicker.SelectedIndex < 0)
                            {
                                return;
                            }

                            _conditionValue = items[conditionValuePicker.SelectedIndex];
                        };

                        conditionValueStackView = conditionValuePicker;
                    }
                    else if (datumTriggerAttribute is DoubleProbeTriggerProperty)
                    {
                        Entry entry = new Entry
                        {
                            Keyboard = Keyboard.Numeric,
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        };

                        entry.TextChanged += (ooo, eee) =>
                        {
                            double value;
                            if (double.TryParse(eee.NewTextValue, out value))
                            {
                                _conditionValue = value;
                            }
                        };

                        conditionValueStackView = entry;
                        allowChangeCalculation = true;
                    }
                    else if (datumTriggerAttribute is StringProbeTriggerProperty)
                    {
                        Entry entry = new Entry
                        {
                            Keyboard = Keyboard.Default,
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        };

                        entry.TextChanged += (ooo, eee) => _conditionValue = eee.NewTextValue;

                        conditionValueStackView = entry;
                        allowRegularExpression = true;
                    }
                    else if (datumTriggerAttribute is BooleanProbeTriggerProperty)
                    {
                        Switch booleanSwitch = new Switch();

                        booleanSwitch.Toggled += (ooo, eee) => _conditionValue = eee.Value;

                        conditionValueStackView = booleanSwitch;
                    }

                    Label conditionValueStackLabel = new Label
                    {
                        Text = "Value:",
                        FontSize = 20,
                        VerticalTextAlignment = TextAlignment.Center
                    };

                    conditionValueStack.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { conditionValueStackLabel, conditionValueStackView }
                    });

                    #region change calculation
                    if (allowChangeCalculation)
                    {
                        Label changeLabel = new Label
                        {
                            Text = "Change:",
                            FontSize = 20,
                            VerticalTextAlignment = TextAlignment.Center
                        };

                        changeSwitch.IsToggled = false;

                        conditionValueStack.Children.Add(new StackLayout
                        {
                            Orientation = StackOrientation.Horizontal,
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            Children = { changeLabel, changeSwitch }
                        });
                    }
                    #endregion

                    #region regular expression
                    if (allowRegularExpression)
                    {
                        Label regexLabel = new Label
                        {
                            Text = "Regular Expression:",
                            FontSize = 20,
                            VerticalTextAlignment = TextAlignment.Center
                        };

                        regexSwitch.IsToggled = false;

                        conditionValueStack.Children.Add(new StackLayout
                        {
                            Orientation = StackOrientation.Horizontal,
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            Children = { regexLabel, regexSwitch }
                        });
                    }
                    #endregion
                };

                datumPropertyPicker.SelectedIndex = 0;
                #endregion

                #region fire repeatedly
                Label fireRepeatedlyLabel = new Label
                {
                    Text = "Fire Repeatedly:",
                    FontSize = 20,
                    VerticalTextAlignment = TextAlignment.Center
                };

                fireRepeatedlySwitch.IsToggled = true;

                triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { fireRepeatedlyLabel, fireRepeatedlySwitch }
                });
                #endregion

                #region start/end times
                Label startTimeLabel = new Label
                {
                    Text = "Start Time:",
                    FontSize = 20,
                    VerticalTextAlignment = TextAlignment.Center
                };

                startTimePicker.Time = new TimeSpan(0, 0, 0);

                triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { startTimeLabel, startTimePicker }
                });

                Label endTimeLabel = new Label
                {
                    Text = "End Time:",
                    FontSize = 20,
                    VerticalTextAlignment = TextAlignment.Center
                };

                endTimePicker.Time = new TimeSpan(23, 59, 59);

                triggerDefinitionLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { endTimeLabel, endTimePicker }
                });
                #endregion
            };

            probePicker.SelectedIndex = 0;

            Button okButton = new Button
            {
                Text = "OK",
                FontSize = 20,
                VerticalOptions = LayoutOptions.Start
            };

            okButton.Clicked += async (o, e) =>
            {
                try
                {
                    _scriptRunner.Triggers.Add(new Probes.User.Scripts.Trigger(_selectedProbe, _selectedDatumProperty, _selectedCondition, _conditionValue, allowChangeCalculation && changeSwitch.IsToggled, fireRepeatedlySwitch.IsToggled, allowRegularExpression && regexSwitch.IsToggled, startTimePicker.Time, endTimePicker.Time));
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync($"Failed to add trigger:  {ex.Message}");
                    SensusServiceHelper.Get().Logger.Log($"Failed to add trigger:  {ex.Message}", LoggingLevel.Normal, GetType());
                }
            };

            contentLayout.Children.Add(okButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
