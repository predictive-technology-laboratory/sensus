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

using SensusService;
using SensusService.Probes;
using SensusService.Probes.User.Scripts;
using SensusService.Probes.User.Scripts.ProbeTriggerProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace SensusUI
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
        /// Initializes a new instance of the <see cref="SensusUI.AddScriptTriggerPage"/> class.
        /// </summary>
        /// <param name="scriptRunner">Script runner to add trigger to.</param>
        public AddScriptTriggerPage(ScriptRunner scriptRunner)
        {
            _scriptRunner = scriptRunner;

            Title = "Add Trigger";

            var enabledProbes = _scriptRunner.Probe.Protocol.Probes.Where(p => p != _scriptRunner.Probe && p.Enabled).ToArray();

            if (!enabledProbes.Any())
            {
                Content = new Label { Text = "No enabled probes. Please enable them before creating triggers.", FontSize = 20 };

                return;
            }

            var contentLayout = new StackLayout
            {
                Orientation     = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            var probeLabel = new Label { Text = "Probe:", FontSize = 20 };

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

            Switch changeSwitch = new Switch();
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
                    return;

                _selectedProbe = enabledProbes[probePicker.SelectedIndex];

                PropertyInfo[] datumProperties = _selectedProbe.DatumType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttributes<ProbeTriggerProperty>().Any()).ToArray();

                if (datumProperties.Length == 0)
                    return;
                    
                #region datum property picker
                Label datumPropertyLabel = new Label
                {
                    Text = "Property:",
                    FontSize = 20
                };

                Picker datumPropertyPicker = new Picker { Title = "Select Datum Property", HorizontalOptions = LayoutOptions.FillAndExpand };
                foreach (PropertyInfo datumProperty in datumProperties)
                {
                    var triggerProperty = datumProperty.GetCustomAttributes<ProbeTriggerProperty>().First();
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
                    FontSize = 20
                };

                Picker conditionPicker = new Picker { Title = "Select Condition", HorizontalOptions = LayoutOptions.FillAndExpand };
                TriggerValueCondition[] conditions = Enum.GetValues(typeof(TriggerValueCondition)) as TriggerValueCondition[];
                foreach (TriggerValueCondition condition in conditions)
                    conditionPicker.Items.Add(condition.ToString());

                conditionPicker.SelectedIndexChanged += (oo, ee) =>
                {
                    if (conditionPicker.SelectedIndex < 0)
                        return;

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
                        return;

                    _selectedDatumProperty = datumProperties[datumPropertyPicker.SelectedIndex];

                    ProbeTriggerProperty datumTriggerAttribute = _selectedDatumProperty.GetCustomAttribute<ProbeTriggerProperty>();

                    View conditionValueStackView = null;
                    bool allowChangeCalculation = false;
                    bool allowRegularExpression = false;

                    if (datumTriggerAttribute is ListProbeTriggerProperty)
                    {
                        Picker conditionValuePicker = new Picker { Title = "Select Condition Value", HorizontalOptions = LayoutOptions.FillAndExpand };
                        object[] items = (datumTriggerAttribute as ListProbeTriggerProperty).Items;
                        foreach (object item in items)
                            conditionValuePicker.Items.Add(item.ToString());

                        conditionValuePicker.SelectedIndexChanged += (ooo, eee) =>
                        {
                            if (conditionValuePicker.SelectedIndex < 0)
                                return;

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
                                _conditionValue = value;
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
                        FontSize = 20
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
                            FontSize = 20
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
                            FontSize = 20
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
                    FontSize = 20
                };

                fireRepeatedlySwitch.IsToggled = false;

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
                    FontSize = 20
                };

                startTimePicker.Time = new TimeSpan(8, 0, 0);

                triggerDefinitionLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { startTimeLabel, startTimePicker }
                    });

                Label endTimeLabel = new Label
                {
                    Text = "End Time:",
                    FontSize = 20
                };

                endTimePicker.Time = new TimeSpan(21, 0, 0);

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
                    _scriptRunner.Triggers.Add(new SensusService.Probes.User.Scripts.Trigger(_selectedProbe, _selectedDatumProperty, _selectedCondition, _conditionValue, changeSwitch.IsToggled, fireRepeatedlySwitch.IsToggled, regexSwitch.IsToggled, startTimePicker.Time, endTimePicker.Time));
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().FlashNotificationAsync($"Failed to add trigger:  {ex.Message}");
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