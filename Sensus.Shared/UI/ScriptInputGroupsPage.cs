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
using Sensus.UI.Inputs;
using Sensus.Probes.User.Scripts;
using System.Linq;
using System.Collections.Generic;

namespace Sensus.UI
{
    public class ScriptInputGroupsPage : ContentPage
    {
        public ScriptInputGroupsPage(Script script)
        {
            Title = "Input Groups";

            ListView groupsList = new ListView(ListViewCachingStrategy.RecycleElement);
            groupsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            groupsList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(InputGroup.Name));
            groupsList.ItemsSource = script.InputGroups;
            groupsList.ItemTapped += async (o, e) =>
            {
                if (groupsList.SelectedItem == null)
                {
                    return;
                }

                InputGroup selectedInputGroup = groupsList.SelectedItem as InputGroup;
                int selectedIndex = script.InputGroups.IndexOf(selectedInputGroup);

                List<string> actions = new string[] { "Edit", "Copy", "Delete" }.ToList();

                if (selectedIndex < script.InputGroups.Count - 1)
                {
                    actions.Insert(0, "Move Down");
                }

                if (selectedIndex > 0)
                {
                    actions.Insert(0, "Move Up");
                }

                string selectedAction = await DisplayActionSheet(selectedInputGroup.Name, "Cancel", null, actions.ToArray());

                if (selectedAction == "Move Up")
                {
                    script.InputGroups.Move(selectedIndex, selectedIndex - 1);
                }
                else if (selectedAction == "Move Down")
                {
                    script.InputGroups.Move(selectedIndex, selectedIndex + 1);
                }
                else if (selectedAction == "Edit")
                {
                    List<InputGroup> previousInputGroups = script.InputGroups.Where((inputGroup, index) => index < selectedIndex).ToList();
                    ScriptInputGroupPage inputGroupPage = new ScriptInputGroupPage(selectedInputGroup, previousInputGroups);
                    await Navigation.PushAsync(inputGroupPage);
                    groupsList.SelectedItem = null;
                }
                else if (selectedAction == "Copy")
                {
                    script.InputGroups.Add(selectedInputGroup.Copy(true));
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedInputGroup.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        script.InputGroups.Remove(selectedInputGroup);
                        groupsList.SelectedItem = null;
                    }
                }
            };

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () => 
            { 
                script.InputGroups.Add(new InputGroup { Name = "New Input Group" }); 
            }));

            Content = groupsList;
        }
    }
}
