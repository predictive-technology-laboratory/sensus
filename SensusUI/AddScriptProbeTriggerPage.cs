#region copyright
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
#endregion

using SensusService;
using SensusService.Probes;
using SensusService.Probes.User;
using SensusService.Probes.User.ProbeTriggerProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace SensusUI
{
    public class AddScriptProbeTriggerPage : ContentPage
    {
        public static event EventHandler TriggerAdded;

        private IScriptProbe _scriptProbe;
        private Probe _selectedProbe;
        private PropertyInfo _selectedDatumProperty;
        private TriggerValueCondition _selectedCondition;
        private object _conditionValue;
        private bool _change;
        private bool _fireRepeatedly;
        private bool _useRegularExpression;

        public AddScriptProbeTriggerPage(IScriptProbe scriptProbe)
        {
            _scriptProbe = scriptProbe;

            Title = "Add Trigger";

            List<Probe> enabledProbes = scriptProbe.Protocol.Probes.Where(p => p != _scriptProbe && p.Enabled).ToList();
            if (enabledProbes.Count == 0)
            {
                Content = new Label
                {
                    Text = "No enabled probes. Please enable them before creating triggers.",
                    Font = Font.SystemFontOfSize(30)
                };

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
                Font = Font.SystemFontOfSize(20)
            };

            Picker probePicker = new Picker { Title = "Select Probe", HorizontalOptions = LayoutOptions.FillAndExpand };
            foreach (Probe enabledProbe in enabledProbes)
                probePicker.Items.Add(enabledProbe.DisplayName);

            contentLayout.Children.Add(new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { probeLabel, probePicker }
            });

            StackLayout triggerDefinitionLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            contentLayout.Children.Add(triggerDefinitionLayout);

            probePicker.SelectedIndexChanged += (o, e) =>
                {
                    if (probePicker.SelectedIndex < 0)
                        return;

                    _selectedProbe = enabledProbes[probePicker.SelectedIndex];

                    triggerDefinitionLayout.Children.Clear();

                    #region datum property picker
                    Type datumType = _selectedProbe.DatumType;

                    Label datumPropertyLabel = new Label
                    {
                        Text = "Property:",
                        Font = Font.SystemFontOfSize(20)
                    };

                    Picker datumPropertyPicker = new Picker { Title = "Select Datum Property", HorizontalOptions = LayoutOptions.FillAndExpand };
                    PropertyInfo[] datumProperties = datumType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttributes<ProbeTriggerProperty>().Count() > 0).ToArray();
                    foreach (PropertyInfo triggerProperty in datumProperties)
                        datumPropertyPicker.Items.Add(triggerProperty.Name);

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
                        Font = Font.SystemFontOfSize(20)
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

                    #region condition value for comparison, based on selected datum property
                    StackLayout conditionValueStack = new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    triggerDefinitionLayout.Children.Add(conditionValueStack);

                    datumPropertyPicker.SelectedIndexChanged += (oo, ee) =>
                        {
                            if (datumPropertyPicker.SelectedIndex < 0)
                                return;

                            _selectedDatumProperty = datumProperties[datumPropertyPicker.SelectedIndex];

                            conditionValueStack.Children.Clear();

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
                            else if (datumTriggerAttribute is NumberProbeTriggerProperty)
                            {
                                Entry entry = new Entry
                                {
                                    Keyboard = Keyboard.Numeric,
                                    HorizontalOptions = LayoutOptions.FillAndExpand
                                };

                                entry.TextChanged += (ooo, eee) =>
                                    {
                                        double value;
                                        if (double.TryParse(eee.NewTextValue, out  value))
                                            _conditionValue = value;
                                    };

                                conditionValueStackView = entry;
                                allowChangeCalculation = true;
                            }
                            else if (datumTriggerAttribute is TextProbeTriggerProperty)
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
                                Font = Font.SystemFontOfSize(20)
                            };

                            conditionValueStack.Children.Add(new StackLayout
                            {
                                Orientation = StackOrientation.Horizontal,
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                                Children = { conditionValueStackLabel, conditionValueStackView }
                            });

                            #region change calculation
                            _change = false;

                            if (allowChangeCalculation)
                            {
                                Label changeLabel = new Label
                                {
                                    Text = "Change:",
                                    Font = Font.SystemFontOfSize(20)
                                };

                                Switch changeSwitch = new Switch
                                {
                                    IsToggled = _change
                                };

                                changeSwitch.Toggled += (ooo, eee) => { _change = eee.Value; };

                                conditionValueStack.Children.Add(new StackLayout
                                {
                                    Orientation = StackOrientation.Horizontal,
                                    HorizontalOptions = LayoutOptions.FillAndExpand,
                                    Children = { changeLabel, changeSwitch }
                                });
                            }
                            #endregion

                            #region regular expression
                            _useRegularExpression = false;

                            if(allowRegularExpression)
                            {
                                Label regexLabel = new Label
                                {
                                    Text = "Regular Expression:",
                                    Font = Font.SystemFontOfSize(20)
                                };

                                Switch regexSwitch = new Switch
                                {
                                    IsToggled = _useRegularExpression
                                };

                                regexSwitch.Toggled += (ooo, eee) => { _useRegularExpression = eee.Value; };

                                conditionValueStack.Children.Add(new StackLayout
                                {
                                    Orientation = StackOrientation.Horizontal,
                                    HorizontalOptions = LayoutOptions.FillAndExpand,
                                    Children = { regexLabel, regexSwitch }
                                });
                            }
                            #endregion

                            #region fire repeatedly
                            _fireRepeatedly = false;

                            Label fireRepeatedlyLabel = new Label
                            {
                                Text = "Fire Repeatedly:",
                                Font = Font.SystemFontOfSize(20)
                            };

                            Switch fireRepeatedlySwitch = new Switch
                            {
                                IsToggled = _fireRepeatedly
                            };

                            fireRepeatedlySwitch.Toggled += (ooo, eee) => { _fireRepeatedly = eee.Value; };

                            conditionValueStack.Children.Add(new StackLayout
                            {
                                Orientation = StackOrientation.Horizontal,
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                                Children = { fireRepeatedlyLabel, fireRepeatedlySwitch }
                            });
                            #endregion
                        };

                    datumPropertyPicker.SelectedIndex = 0;
                    #endregion
                };

            probePicker.SelectedIndex = 0;

            Button okButton = new Button
            {
                Text = "OK",
                Font = Font.SystemFontOfSize(20)
            };

            okButton.Clicked += (o, e) =>
                {
                    try
                    {
                        _scriptProbe.Triggers.Add(new Trigger(_selectedProbe, _selectedDatumProperty.Name, _selectedCondition, _conditionValue, _change, _fireRepeatedly, _useRegularExpression));

                        if (TriggerAdded != null)
                            TriggerAdded(o, e);
                    }
                    catch (Exception ex)
                    {
                        string message = "Failed to add trigger:  " + ex.Message;
                        UiBoundSensusServiceHelper.Get().FlashNotificationAsync(message);
                        UiBoundSensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal);
                    }
                };

            contentLayout.Children.Add(okButton);

            Content = contentLayout;
        }
    }
}
