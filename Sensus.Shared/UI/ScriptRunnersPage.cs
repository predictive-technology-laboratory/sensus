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
