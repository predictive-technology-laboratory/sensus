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
using SensusUI.Inputs;

namespace SensusUI
{
    public class ScriptInputGroupsPage : ContentPage
    {
        private Script _script;
        private ListView _groupsList;

        public ScriptInputGroupsPage(Script script)
        {
            _script = script;

            Title = "Input Groups";

            _groupsList = new ListView();
            _groupsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _groupsList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            _groupsList.ItemTapped += async (o, e) =>
            {
                if (_groupsList.SelectedItem == null)
                    return;

                InputGroup selectedInputGroup = _groupsList.SelectedItem as InputGroup;

                string selectedAction = await DisplayActionSheet(selectedInputGroup.Name, "Cancel", null, "Edit", "Delete");

                if (selectedAction == "Edit")
                {
                    ScriptInputGroupPage inputGroupPage = new ScriptInputGroupPage(selectedInputGroup);

                    inputGroupPage.Disappearing += (oo, ee) =>
                    {
                        Bind();
                    };

                    await Navigation.PushAsync(inputGroupPage);

                    _groupsList.SelectedItem = null;
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedInputGroup.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        _script.InputGroups.Remove(selectedInputGroup);
                        _groupsList.SelectedItem = null;  // manually reset, since it isn't done automatically.
                    }
                }
            };

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                    {
                        _script.InputGroups.Add(new InputGroup("New Input Group"));
                    }));

            Bind();
            Content = _groupsList;
        }

        public void Bind()
        {
            _groupsList.ItemsSource = null;
            _groupsList.ItemsSource = _script.InputGroups;
        }
    }
}