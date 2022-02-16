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

using Xamarin.Forms;
using Sensus.UI.Inputs;
using Sensus.Probes.User.Scripts;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Sensus.UI
{
	public class ScriptInputGroupsPage : ContentPage
	{
		public ScriptInputGroupsPage(Script script)
		{
			Title = "Input Groups";

			ListView groupsList = new ListView(ListViewCachingStrategy.RecycleElement);
			groupsList.ItemTemplate = new DataTemplate(typeof(DarkModeCompatibleTextCell));
			groupsList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(InputGroup.ListItemText));
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

				string selectedAction = await DisplayActionSheet(selectedInputGroup.ListItemText, "Cancel", null, actions.ToArray());

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
					ScriptInputGroupPage inputGroupPage = new ScriptInputGroupPage(selectedInputGroup, previousInputGroups, script);
					await Navigation.PushAsync(inputGroupPage);
					groupsList.SelectedItem = null;
				}
				else if (selectedAction == "Copy")
				{
					script.InputGroups.Add(selectedInputGroup.Copy(true));
				}
				else if (selectedAction == "Delete")
				{
					if (await DisplayAlert("Delete " + selectedInputGroup.ListItemText + "?", "This action cannot be undone.", "Delete", "Cancel"))
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