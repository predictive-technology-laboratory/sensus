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
using Xamarin.Forms;
using SensusService.Probes.User;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SensusUI.Inputs;
using System.Reflection;
using System.Linq;
using SensusService;

namespace SensusUI
{
    public class ScriptInputsPage : ContentPage
    {
        private InputGroup _inputGroup;
        private ListView _inputsList;

        public ScriptInputsPage(InputGroup inputGroup, List<InputGroup> previousInputGroups)
        {
            _inputGroup = inputGroup;

            Title = "Inputs";

            _inputsList = new ListView();
            _inputsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _inputsList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            _inputsList.ItemTapped += async (o, e) =>
            {
                if (_inputsList.SelectedItem == null)
                    return;

                Input selectedInput = _inputsList.SelectedItem as Input;
                int selectedIndex = inputGroup.Inputs.IndexOf(selectedInput);

                List<string> actions = new string[] { "Edit" }.ToList();

                if (previousInputGroups != null && previousInputGroups.Count > 0)
                    actions.AddRange(new string[] { "Add Display Condition", "View Display Conditions", "Clear Display Conditions" });

                if (selectedIndex < inputGroup.Inputs.Count - 1)
                    actions.Insert(0, "Move Down");

                if (selectedIndex > 0)
                    actions.Insert(0, "Move Up");

                actions.Add("Delete");
                    
                string selectedAction = await DisplayActionSheet(selectedInput.Name, "Cancel", null, actions.ToArray());

                if (selectedAction == "Move Up")
                    inputGroup.Inputs.Move(selectedIndex, selectedIndex - 1);
                else if (selectedAction == "Move Down")
                    inputGroup.Inputs.Move(selectedIndex, selectedIndex + 1);
                else if (selectedAction == "Edit")
                {
                    ScriptInputPage inputPage = new ScriptInputPage(selectedInput);
                    inputPage.Disappearing += (oo, ee) =>
                    {
                        Bind();
                    };
                        
                    await Navigation.PushAsync(inputPage);
                    _inputsList.SelectedItem = null;
                }
                else if (selectedAction == "Add Display Condition")
                {
                    SensusServiceHelper.Get().PromptForInputsAsync("Display Condition", new Input[]
                        {
                            new ItemPickerPageInput("Input to condition on:", previousInputGroups.SelectMany(previousInputGroup => previousInputGroup.Inputs.Cast<object>()).ToList(), null),
                            new ItemPickerPageInput("Condition:", Enum.GetValues(typeof(InputValueCondition)).Cast<object>().ToList(), null),
                            new TextInput("Value:", Keyboard.Default) { Required = false },
                            new ItemPickerPageInput("Conjunctive:", new object[] { true, false }.ToList(), null)
                        },
                        null,
                        true,
                        "OK",
                        null,
                        "Condition not complete. Cancel?",
                        null,
                        inputs =>
                        {
                            if (inputs == null)
                                return;

                            if (inputs.All(input => input.Complete))
                            {
                                Input input = (inputs[0] as ItemPickerPageInput).Value as Input;
                                InputValueCondition condition = (InputValueCondition)(inputs[1] as ItemPickerPageInput).Value;
                                string value = (inputs[2] as TextInput).Value as string;
                                bool conjunction = (bool)(inputs[3] as ItemPickerPageInput).Value;
                                selectedInput.DisplayConditions.Add(new InputDisplayCondition(input, condition, value, conjunction));
                            }
                        });
                }
                else if (selectedAction == "View Display Conditions")
                {
                    await Navigation.PushAsync(new ViewTextLinesPage("Display Conditions", selectedInput.DisplayConditions.Select(displayCondition => displayCondition.ToString()).ToList(), null));
                }
                else if (selectedAction == "Clear Display Conditions")
                {
                    if (await DisplayAlert("Confirm", "Clear all display conditions?", "Yes", "No"))
                        selectedInput.DisplayConditions.Clear();
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedInput.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        _inputGroup.Inputs.Remove(selectedInput);
                        _inputsList.SelectedItem = null;  // manually reset, since it isn't done automatically.
                    }
                }
            };

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
                    {
                        List<Input> inputs = Assembly.GetExecutingAssembly()
                            .GetTypes()
                            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Input)))
                            .Select(t => Activator.CreateInstance(t))
                            .Cast<Input>()
                            .OrderBy(i => i.Name)
                            .ToList();

                        string cancelButtonName = "Cancel";
                        string selected = await DisplayActionSheet("Select Input Type", cancelButtonName, null, inputs.Select((input, index) => (index + 1) + ") " + input.Name).ToArray());
                        if (!string.IsNullOrWhiteSpace(selected) && selected != cancelButtonName)
                        {
                            Input input = inputs[int.Parse(selected.Substring(0, selected.IndexOf(")"))) - 1];

                            if (input is VoiceInput && inputGroup.Inputs.Count > 0 || !(input is VoiceInput) && inputGroup.Inputs.Any(i => i is VoiceInput))
                                SensusServiceHelper.Get().FlashNotificationAsync("Voice inputs must reside in groups by themselves.");
                            else
                            {
                                inputGroup.Inputs.Add(input);

                                ScriptInputPage inputPage = new ScriptInputPage(input);
                                inputPage.Disappearing += (o, e) =>
                                {
                                    Bind();
                                };
                        
                                await Navigation.PushAsync(inputPage);
                            }
                        }
                    }));

            Bind();
            Content = _inputsList;
        }

        public void Bind()
        {
            _inputsList.ItemsSource = null;
            _inputsList.ItemsSource = _inputGroup.Inputs;
        }
    }
}