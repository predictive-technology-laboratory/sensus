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
    public class ScriptTriggersPage : ContentPage
    {
        public ScriptTriggersPage(Script script)
        {
            Title = "Script Triggers";

            ListView triggerList = new ListView();
            triggerList.ItemTemplate = new DataTemplate(typeof(TextCell));
            triggerList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            triggerList.ItemsSource = script.Triggers;

            Content = triggerList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
                {
                    if (script.Probe.Protocol.Probes.Where(p => p != script.Probe && p.Enabled).Count() > 0)
                        await Navigation.PushAsync(new AddScriptTriggerPage(script));
                    else
                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("You must enable other probes before adding triggers.");
                }));

            ToolbarItems.Add(new ToolbarItem(null, "minus.png", async () =>
                {
                    if (triggerList.SelectedItem != null && await DisplayAlert("Confirm Delete", "Are you sure you want to delete the selected trigger?", "Yes", "Cancel"))
                    {
                        script.Triggers.Remove(triggerList.SelectedItem as SensusService.Probes.User.Trigger);
                        triggerList.SelectedItem = null;
                    }
                }));
        }
    }
}
