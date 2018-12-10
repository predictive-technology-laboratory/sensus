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

using System.Linq;
using Sensus.Probes.User.Scripts;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Displays  triggers for a script runner.
    /// </summary>
    public class ScriptTriggersPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptTriggersPage"/> class.
        /// </summary>
        /// <param name="scriptRunner">Script runner to display.</param>
        public ScriptTriggersPage(ScriptRunner scriptRunner)
        {
            Title = "Script Triggers";

            ListView triggerList = new ListView(ListViewCachingStrategy.RecycleElement);
            triggerList.ItemTemplate = new DataTemplate(typeof(TextCell));
            triggerList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            triggerList.ItemsSource = scriptRunner.Triggers;
            triggerList.ItemTapped += async (o, e) =>
            {
                if (triggerList.SelectedItem == null)
                {
                    return;
                }

                Probes.User.Scripts.Trigger selectedTrigger = triggerList.SelectedItem as Probes.User.Scripts.Trigger;

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
                {
                    await Navigation.PushAsync(new AddScriptTriggerPage(scriptRunner));
                }
                else
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("You must enable other probes before adding triggers.");
                }
            }));
        }
    }
}
