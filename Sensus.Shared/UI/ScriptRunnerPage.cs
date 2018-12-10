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

using Xamarin.Forms;
using Sensus.UI.UiProperties;
using Sensus.Probes.User.Scripts;
using System.Linq;

namespace Sensus.UI
{
    /// <summary>
    /// Displays a script runner, allowing the user to edit its prompts and triggers.
    /// </summary>
    public class ScriptRunnerPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptRunnerPage"/> class.
        /// </summary>
        /// <param name="scriptRunner">Script runner to display.</param>
        public ScriptRunnerPage(ScriptRunner scriptRunner)
        {
            Title = "Script";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(scriptRunner))
            {
                contentLayout.Children.Add(stack);
            }

            Button editInputGroupsButton = new Button
            {
                Text = "Edit Input Groups",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editInputGroupsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ScriptInputGroupsPage(scriptRunner.Script));
            };

            contentLayout.Children.Add(editInputGroupsButton);

            Button editTriggersButton = new Button
            {
                Text = "Edit Triggers",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editTriggersButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ScriptTriggersPage(scriptRunner));
            };

            contentLayout.Children.Add(editTriggersButton);

            Button viewScheduledTriggersButton = new Button
            {
                Text = "View Scheduled Triggers",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            viewScheduledTriggersButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ViewTextLinesPage("Scheduled Triggers", scriptRunner.ScriptRunCallbacks.Select(scheduledCallback =>
                {
                    return scheduledCallback.Id + " (" + scheduledCallback.State + "):  " + (scheduledCallback.NextExecution == null ? "Next execution date and time have not been set." : scheduledCallback.NextExecution.ToString());

                }).ToList()));
            };

            contentLayout.Children.Add(viewScheduledTriggersButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
