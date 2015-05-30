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

namespace SensusUI
{
    public class PromptsPage : ContentPage
    {
        private ObservableCollection<Prompt> _prompts;
        private ListView _promptsList;

        public PromptsPage(ObservableCollection<Prompt> prompts)
        {
            _prompts = prompts;

            Title = "Prompts";

            _promptsList = new ListView();
            _promptsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _promptsList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            _promptsList.ItemTapped += async (o, e) =>
            {
                if (_promptsList.SelectedItem == null)
                    return;

                Prompt selectedPrompt = _promptsList.SelectedItem as Prompt;

                string selectedAction = await DisplayActionSheet(selectedPrompt.Name, "Cancel", null, "Edit", "Delete");

                if (selectedAction == "Edit")
                {
                    PromptPage promptPage = new PromptPage(selectedPrompt);
                    promptPage.Disappearing += (oo, ee) =>
                    {
                        Bind();
                    };
                        
                    await Navigation.PushAsync(promptPage);
                    _promptsList.SelectedItem = null;
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedPrompt.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        _prompts.Remove(selectedPrompt);
                        _promptsList.SelectedItem = null;  // manually reset, since it isn't done automatically.
                    }
                }
            };

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                {
                    _prompts.Add(new Prompt("New Prompt", PromptOutputType.Text, "", "", PromptInputType.Text));
                }));

            Bind();
            Content = _promptsList;
        }

        public void Bind()
        {
            _promptsList.ItemsSource = null;
            _promptsList.ItemsSource = _prompts;
        }
    }
}


