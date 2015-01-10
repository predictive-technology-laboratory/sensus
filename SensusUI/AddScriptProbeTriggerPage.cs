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

using SensusService.Probes;
using Xamarin.Forms;
using System.Linq;
using SensusService.Probes.User;
using SensusService;
using System.Collections.Generic;
using System;
using System.Reflection;
using SensusService.Probes.User.ProbeTriggerProperties;

namespace SensusUI
{
    public class AddScriptProbeTriggerPage : ContentPage
    {
        private IScriptProbe _scriptProbe;
        private Probe _selectedProbe;
        private PropertyInfo _selectedDatumProperty;
        private TriggerValueCondition _selectedCondition;
        private object _conditionValue;
        private bool _change;

        public AddScriptProbeTriggerPage(IScriptProbe scriptProbe)
        {
            _scriptProbe = scriptProbe;

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

            Picker probePicker = new Picker();


            foreach (Probe enabledProbe in enabledProbes)
                probePicker.Items.Add(enabledProbe.GetType().FullName);

            contentLayout.Children.Add(probePicker);

            StackLayout triggerLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            contentLayout.Children.Add(triggerLayout);

            probePicker.SelectedIndexChanged += (o, e) =>
                {
                    if (probePicker.SelectedIndex < 0)
                        return;

                    _selectedProbe = enabledProbes.Where(p => p.GetType().FullName == probePicker.Items[probePicker.SelectedIndex]).First();

                    triggerLayout.Children.Clear();

                    #region property picker based on probe's datum type
                    Type datum = _selectedProbe.DatumType;

                    Label propertyLabel = new Label
                    {
                        Text = "Property:",
                        Font = Font.SystemFontOfSize(20)
                    };

                    Picker propertyPicker = new Picker();
                    PropertyInfo[] triggerProperties = datum.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttributes<ProbeTriggerProperty>().Count() > 0).ToArray();
                    foreach (PropertyInfo triggerProperty in triggerProperties)
                        propertyPicker.Items.Add(triggerProperty.Name);

                    triggerLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { propertyLabel, propertyPicker }
                    });
                    #endregion

                    #region condition (same for all datum types)
                    Label conditionLabel = new Label
                    {
                        Text = "Condition:",
                        Font = Font.SystemFontOfSize(20)
                    };

                    Picker conditionPicker = new Picker();
                    foreach (TriggerValueCondition condition in Enum.GetValues(typeof(TriggerValueCondition)))
                        conditionPicker.Items.Add(conditionPicker.ToString());

                    conditionPicker.SelectedIndexChanged += (oo, ee) =>
                        {
                            if (conditionPicker.SelectedIndex < 0)
                                return;

                            _selectedCondition = (TriggerValueCondition)Enum.Parse(typeof(TriggerValueCondition), conditionPicker.Items[conditionPicker.SelectedIndex]);
                        };

                    triggerLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { conditionLabel, conditionPicker }
                    });
                    #endregion

                    #region property value, based on selected property
                    StackLayout propertyValueStack = new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    triggerLayout.Children.Add(propertyValueStack);

                    propertyPicker.SelectedIndexChanged += (oo, ee) =>
                        {
                            if (propertyPicker.SelectedIndex < 0)
                                return;

                            _selectedDatumProperty = triggerProperties.Where(p => p.Name == propertyPicker.Items[propertyPicker.SelectedIndex]).First();

                            propertyValueStack.Children.Clear();

                            ProbeTriggerProperty triggerAttribute = _selectedDatumProperty.GetCustomAttribute<ProbeTriggerProperty>();

                            View propertyValueStackView = null;
                            bool allowChangeCalculation = false;

                            if (triggerAttribute is ListProbeTriggerProperty)
                            {
                                Picker picker = new Picker();
                                object[] items = (triggerAttribute as ListProbeTriggerProperty).Items;
                                foreach (object item in items)
                                    picker.Items.Add(item.ToString());

                                picker.SelectedIndexChanged += (ooo, eee) =>
                                    {
                                        if (picker.SelectedIndex < 0)
                                            return;

                                        _conditionValue = items.Where(i => i.ToString() == picker.Items[picker.SelectedIndex].ToString());
                                    };

                                propertyValueStackView = picker;
                            }
                            else if (triggerAttribute is NumberProbeTriggerProperty)
                            {
                                Entry entry = new Entry
                                {
                                    Keyboard = Keyboard.Numeric,
                                    HorizontalOptions = LayoutOptions.FillAndExpand
                                };

                                entry.TextChanged += (ooo, eee) => _conditionValue = double.Parse(entry.Text);

                                propertyValueStackView = entry;
                                allowChangeCalculation = true;
                            }
                            else if (triggerAttribute is TextProbeTriggerProperty)
                            {
                                Entry entry = new Entry
                                {
                                    Keyboard = Keyboard.Default,
                                    HorizontalOptions = LayoutOptions.FillAndExpand
                                };

                                entry.TextChanged += (ooo, eee) => _conditionValue = entry.Text;

                                propertyValueStackView = entry;
                            }

                            propertyValueStack.Children.Add(propertyValueStackView);

                            if (allowChangeCalculation)
                            {
                                Label switchLabel = new Label
                                {
                                    Text = "Change:",
                                    Font = Font.SystemFontOfSize(20)
                                };

                                Switch changeSwitch = new Switch();

                                changeSwitch.Toggled += (ooo, eee) => { _change = eee.Value; };

                                propertyValueStack.Children.Add(new StackLayout
                                {
                                    Orientation = StackOrientation.Horizontal,
                                    HorizontalOptions = LayoutOptions.FillAndExpand,
                                    Children = { switchLabel, changeSwitch }
                                });
                            }

                            _change = false;
                        };

                    propertyPicker.SelectedIndex = 0;
                    #endregion
                };

            probePicker.SelectedIndex = 0;

            Button okButton = new Button
            {
                Text = "OK",
                Font = Font.SystemFontOfSize(20)
            };

            okButton.Clicked += AddTrigger;

            contentLayout.Children.Add(okButton);

            Content = contentLayout;
        }

        private void AddTrigger(object sender, EventArgs args)
        {
            _scriptProbe.AddTrigger(_selectedProbe, _selectedDatumProperty, _selectedCondition, _conditionValue, _change);
        }
    }
}
