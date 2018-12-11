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

using Xamarin.Forms;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sensus.UI.Inputs;

namespace Sensus.UI
{
    public class ScriptInputsPage : ContentPage
    {
        public ScriptInputsPage(InputGroup inputGroup, List<InputGroup> previousInputGroups)
        {
            Title = "Inputs";

            ListView inputsList = new ListView(ListViewCachingStrategy.RecycleElement);
            inputsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            inputsList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Input.Caption));
            inputsList.ItemsSource = inputGroup.Inputs;
            inputsList.ItemTapped += async (o, e) =>
            {
                if (inputsList.SelectedItem == null)
                {
                    return;
                }

                Input selectedInput = inputsList.SelectedItem as Input;
                int selectedIndex = inputGroup.Inputs.IndexOf(selectedInput);

                List<string> actions = new List<string>();

                if (selectedIndex > 0)
                {
                    actions.Add("Move Up");
                }

                if (selectedIndex < inputGroup.Inputs.Count - 1)
                {
                    actions.Add("Move Down");
                }

                actions.Add("Edit");

                if (previousInputGroups != null && previousInputGroups.Select(previousInputGroup => previousInputGroup.Inputs.Count).Sum() > 0)
                {
                    actions.Add("Add Display Condition");
                }

                if (selectedInput.DisplayConditions.Count > 0)
                {
                    actions.AddRange(new string[] { "View Display Conditions" });
                }

                actions.Add("Delete");

                string selectedAction = await DisplayActionSheet(selectedInput.Name, "Cancel", null, actions.ToArray());

                if (selectedAction == "Move Up")
                {
                    inputGroup.Inputs.Move(selectedIndex, selectedIndex - 1);
                }
                else if (selectedAction == "Move Down")
                {
                    inputGroup.Inputs.Move(selectedIndex, selectedIndex + 1);
                }
                else if (selectedAction == "Edit")
                {
                    ScriptInputPage inputPage = new ScriptInputPage(selectedInput);
                    await Navigation.PushAsync(inputPage);
                    inputsList.SelectedItem = null;
                }
                else if (selectedAction == "Add Display Condition")
                {
                    string abortMessage = "Condition is not complete. Abort?";

                    List<Input> inputs = await SensusServiceHelper.Get().PromptForInputsAsync("Display Condition", new Input[]
                    {
                        new ItemPickerPageInput("Input:", previousInputGroups.SelectMany(previousInputGroup => previousInputGroup.Inputs.Cast<object>()).ToList()),
                        new ItemPickerPageInput("Condition:", Enum.GetValues(typeof(InputValueCondition)).Cast<object>().ToList()),
                        new ItemPickerPageInput("Conjunctive/Disjunctive:", new object[] { "Conjunctive", "Disjunctive" }.ToList())

                    }, null, true, null, null, abortMessage, null, false);

                    if (inputs == null)
                    {
                        return;
                    }

                    if (inputs.All(input => input.Valid))
                    {
                        Input conditionInput = ((inputs[0] as ItemPickerPageInput).Value as IEnumerable<object>).First() as Input;
                        InputValueCondition condition = (InputValueCondition)((inputs[1] as ItemPickerPageInput).Value as IEnumerable<object>).First();
                        bool conjunctive = ((inputs[2] as ItemPickerPageInput).Value as IEnumerable<object>).First().Equals("Conjunctive");

                        if (condition == InputValueCondition.IsComplete)
                        {
                            selectedInput.DisplayConditions.Add(new InputDisplayCondition(conditionInput, condition, null, conjunctive));
                        }
                        else
                        {
                            Regex uppercaseSplitter = new Regex(@"
                                    (?<=[A-Z])(?=[A-Z][a-z]) |
                                    (?<=[^A-Z])(?=[A-Z]) |
                                    (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

                            // show the user a required copy of the condition input and prompt for the condition value
                            Input conditionInputCopy = conditionInput.Copy(false);
                            conditionInputCopy.DisplayConditions.Clear();
                            conditionInputCopy.LabelText = "Value that " + conditionInputCopy.Name + " " + uppercaseSplitter.Replace(condition.ToString(), " ").ToLower() + ":";
                            conditionInputCopy.Required = true;

                            // ensure that the copied input cannot define a variable
                            if (conditionInputCopy is IVariableDefiningInput)
                            {
                                (conditionInputCopy as IVariableDefiningInput).DefinedVariable = null;
                            }

                            Input input = await SensusServiceHelper.Get().PromptForInputAsync("Display Condition", conditionInputCopy, null, true, "OK", null, abortMessage, null, false);

                            if (input?.Valid ?? false)
                            {
                                selectedInput.DisplayConditions.Add(new InputDisplayCondition(conditionInput, condition, input.Value, conjunctive));
                            }
                        }
                    }
                }
                else if (selectedAction == "View Display Conditions")
                {
                    await Navigation.PushAsync(new ViewTextLinesPage("Display Conditions", selectedInput.DisplayConditions.Select(displayCondition => displayCondition.ToString()).ToList(), () =>
                    {
                        selectedInput.DisplayConditions.Clear();
                    }));
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedInput.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        inputGroup.Inputs.Remove(selectedInput);
                        inputsList.SelectedItem = null;  // manually reset, since it isn't done automatically.
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
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Voice inputs must reside in groups by themselves.");
                    }
                    else
                    {
                        inputGroup.Inputs.Add(input);
                        await Navigation.PushAsync(new ScriptInputPage(input));
                    }
                }
            }));

            Content = inputsList;
        }
    }
}
