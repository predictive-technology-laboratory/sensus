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
    /// <summary>
    /// Displays script runners for a script probe.
    /// </summary>
    public class ScriptRunnersPage : ContentPage
    {
        private ScriptProbe _probe;
        private ListView _scriptRunnersList;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensusUI.ScriptRunnersPage"/> class.
        /// </summary>
        /// <param name="probe">Probe to display.</param>
        public ScriptRunnersPage(ScriptProbe probe)
        {
            _probe = probe;

            Title = "Scripts";

            _scriptRunnersList = new ListView();
            _scriptRunnersList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _scriptRunnersList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            _scriptRunnersList.ItemTapped += async (o, e) =>
            {
                if (_scriptRunnersList.SelectedItem == null)
                    return;

                ScriptRunner selectedScriptRunner = _scriptRunnersList.SelectedItem as ScriptRunner;

                string selectedAction = await DisplayActionSheet(selectedScriptRunner.Name, "Cancel", null, "Edit", "Delete");

                if (selectedAction == "Edit")
                {
                    ScriptRunnerPage scriptRunnerPage = new ScriptRunnerPage(selectedScriptRunner);
                    scriptRunnerPage.Disappearing += (oo, ee) =>
                    {
                        Bind();
                    };

                    await Navigation.PushAsync(scriptRunnerPage);
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedScriptRunner.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        selectedScriptRunner.Stop();
                        _probe.ScriptRunners.Remove(selectedScriptRunner);
                        _scriptRunnersList.SelectedItem = null;  // reset manually since it's not done automatically
                    }
                }
            };

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                    {
                        _probe.ScriptRunners.Add(new ScriptRunner("New Script", _probe));
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