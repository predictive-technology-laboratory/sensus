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
        public ScriptTriggersPage(ScriptProbe probe)
        {
            Title = "Script Triggers";

            ScriptProbe scriptProbe = probe as ScriptProbe;

            ListView triggerList = new ListView();
            triggerList.ItemTemplate = new DataTemplate(typeof(TextCell));
            triggerList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            triggerList.ItemsSource = scriptProbe.Triggers;

            Content = triggerList;

            ToolbarItems.Add(new ToolbarItem("+", null, async () =>
                {
                    if (scriptProbe.Protocol.Probes.Where(p => p != scriptProbe && p.Enabled).Count() > 0)
                        await Navigation.PushAsync(new AddScriptProbeTriggerPage(scriptProbe));
                    else
                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("You must enable probes before adding triggers.");
                }));

            ToolbarItems.Add(new ToolbarItem("-", null, async () =>
                {
                    if (triggerList.SelectedItem != null && await DisplayAlert("Confirm Delete", "Are you sure you want to delete the selected trigger?", "Yes", "Cancel"))
                    {
                        scriptProbe.Triggers.Remove(triggerList.SelectedItem as SensusService.Probes.User.Trigger);
                        triggerList.SelectedItem = null;
                    }
                }));
        }
    }
}
