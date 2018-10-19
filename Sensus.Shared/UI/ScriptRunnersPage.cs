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

using Sensus.Probes.User.Scripts;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Displays script runners for a script probe.
    /// </summary>
    public class ScriptRunnersPage : ContentPage
    {
        public ScriptRunnersPage(ScriptProbe probe)
        {
            Title = "Scripts";

            ListView scriptRunnersList = new ListView(ListViewCachingStrategy.RecycleElement);
            scriptRunnersList.ItemTemplate = new DataTemplate(typeof(TextCell));
            scriptRunnersList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(ScriptRunner.Caption));
            scriptRunnersList.ItemsSource = probe.ScriptRunners;
            scriptRunnersList.ItemTapped += async (o, e) =>
            {
                if (scriptRunnersList.SelectedItem == null)
                {
                    return;
                }

                ScriptRunner selectedScriptRunner = scriptRunnersList.SelectedItem as ScriptRunner;

                string selectedAction = await DisplayActionSheet(selectedScriptRunner.Name, "Cancel", null, "Edit", "Delete");

                if (selectedAction == "Edit")
                {
                    await Navigation.PushAsync(new ScriptRunnerPage(selectedScriptRunner));
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedScriptRunner.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        await selectedScriptRunner.StopAsync();
                        selectedScriptRunner.Enabled = false;
                        selectedScriptRunner.Triggers.Clear();

                        probe.ScriptRunners.Remove(selectedScriptRunner);

                        scriptRunnersList.SelectedItem = null;  // reset manually since it's not done automatically
                    }
                }
            };

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
            {
                probe.ScriptRunners.Add(new ScriptRunner("New Script", probe));
            }));

            Content = scriptRunnersList;
        }
    }
}