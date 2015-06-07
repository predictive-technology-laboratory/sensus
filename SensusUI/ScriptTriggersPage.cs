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

using SensusService.Probes.User;
using System.Linq;
using Xamarin.Forms;

namespace SensusUI
{
    /// <summary>
    /// Displays  triggers for a script runner.
    /// </summary>
    public class ScriptTriggersPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SensusUI.ScriptTriggersPage"/> class.
        /// </summary>
        /// <param name="scriptRunner">Script runner to display.</param>
        public ScriptTriggersPage(ScriptRunner scriptRunner)
        {
            Title = "Script Triggers";

            ListView triggerList = new ListView();
            triggerList.ItemTemplate = new DataTemplate(typeof(TextCell));
            triggerList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            triggerList.ItemsSource = scriptRunner.Triggers;
            triggerList.ItemTapped += async (o, e) =>
            {
                if (triggerList.SelectedItem == null)
                    return;

                SensusService.Probes.User.Trigger selectedTrigger = triggerList.SelectedItem as SensusService.Probes.User.Trigger;

                string selectedAction = await DisplayActionSheet(selectedTrigger.ToString(), "Cancel", null, "Delete");

                if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Confirm Delete", "Are you sure you want to delete the selected trigger?", "Yes", "Cancel"))
                    {
                        scriptRunner.Triggers.Remove(selectedTrigger);
                        triggerList.SelectedItem = null;  // must reset this, since it isn't reset automatically
                    }
                }
            };
            
            Content = triggerList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
                    {
                        if (scriptRunner.Probe.Protocol.Probes.Where(p => p != scriptRunner.Probe && p.Enabled).Count() > 0)
                            await Navigation.PushAsync(new AddScriptTriggerPage(scriptRunner));
                        else
                            UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("You must enable other probes before adding triggers.");
                    }));
        }
    }
}