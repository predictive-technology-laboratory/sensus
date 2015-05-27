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
using System.Collections.ObjectModel;
using SensusService.Probes.User;
using Xamarin.Forms;

namespace SensusUI
{
    public class ScriptRunnersPage : ContentPage
    {
        private ScriptProbe _probe;
        private ListView _scriptRunnersList;

        public ScriptRunnersPage(ScriptProbe probe)
        {
            _probe = probe;

            Title = "Scripts";

            _scriptRunnersList = new ListView();
            _scriptRunnersList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _scriptRunnersList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                    {
                        _probe.ScriptRunners.Add(new ScriptRunner("New Script", _probe));
                    }));

            ToolbarItems.Add(new ToolbarItem(null, "minus.png", async () =>
                    {
                        if (_scriptRunnersList.SelectedItem != null)
                        {
                            ScriptRunner scriptRunnerToDelete = _scriptRunnersList.SelectedItem as ScriptRunner;

                            if (await DisplayAlert("Delete " + scriptRunnerToDelete.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                            {
                                scriptRunnerToDelete.Stop();
                                _probe.ScriptRunners.Remove(scriptRunnerToDelete);
                            }
                        }
                    }));

            ToolbarItems.Add(new ToolbarItem(null, "pencil.png", async () =>
                    {
                        if (_scriptRunnersList.SelectedItem != null)
                        {
                            ScriptRunnerPage scriptRunnerPage = new ScriptRunnerPage(_scriptRunnersList.SelectedItem as ScriptRunner);
                            scriptRunnerPage.Disappearing += (o, e) =>
                            {
                                Bind();
                            };
                        
                            await Navigation.PushAsync(scriptRunnerPage);
                        }
                    }));

            Bind();
            Content = _scriptRunnersList;
        }

        private void Bind()
        {
            _scriptRunnersList.ItemsSource = null;
            _scriptRunnersList.ItemsSource = _probe.ScriptRunners;
        }
    }
}