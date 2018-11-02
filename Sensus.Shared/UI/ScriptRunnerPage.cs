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

using Xamarin.Forms;
using Sensus.UI.UiProperties;
using Sensus.Probes.User.Scripts;
using System.Linq;
using Sensus.UI.Inputs;
using System.Threading.Tasks;
using System.Net;
using System.Reflection;
using System.Collections.Generic;
using System;

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

            Button setAgentButton = new Button
            {
                Text = "Set Agent",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            setAgentButton.Clicked += async (o, e) =>
            {
                QrCodeInput agentUrlQrCodeInput = await SensusServiceHelper.Get().PromptForInputAsync("Survey Agent", new QrCodeInput(QrCodePrefix.SURVEY_AGENT, "URL:", false, "Agent URL:")
                {
                    Required = true

                }, null, true, "Set", null, null, null, false) as QrCodeInput;

                string url = agentUrlQrCodeInput?.Value.ToString();
                if(string.IsNullOrWhiteSpace(url))
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Agent not set.");
                    return;
                }

                TaskCompletionSource<ScriptRunnerAgent> completionSource = new TaskCompletionSource<ScriptRunnerAgent>();
                WebClient downloadClient = new WebClient();
                Uri webURI = new Uri(url);

                downloadClient.DownloadDataCompleted += async (downloadSender, downloadEventArgs) =>
                {
                    string errorMessage = null;

                    if (downloadEventArgs.Error == null)
                    {
                        Assembly assembly = Assembly.Load(downloadEventArgs.Result);

                        List<ScriptRunnerAgent> agents = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ScriptRunnerAgent)) && !t.IsAbstract).Select(Activator.CreateInstance).Cast<ScriptRunnerAgent>().ToList();

                        if (agents.Count == 0)
                        {
                            errorMessage = "No agents were present in the specified file.";
                        }
                        else if (agents.Count == 1)
                        {
                            ScriptRunnerAgent agent = agents[0];

                            if (await DisplayAlert("Survey Agent", "Would you like to use the following agent?" + Environment.NewLine + Environment.NewLine + agent.Name, "Yes", "No"))
                            {
                                completionSource.SetResult(agents[0]);
                            }
                            else
                            {
                                errorMessage = "Declined agent.";
                            }
                        }
                        else
                        {
                            ItemPickerPageInput agentPicker = await SensusServiceHelper.Get().PromptForInputAsync("Select Agent", new ItemPickerPageInput("Agents", agents.Cast<object>().ToList(), "Name")
                            {
                                Required = true

                            }, null, true, "OK", null, null, null, false) as ItemPickerPageInput;

                            completionSource.SetResult(agentPicker.Value as ScriptRunnerAgent);
                        }
                    }
                    else
                    {
                        errorMessage = "Failed to download agent from URI \"" + webURI + "\". If this is an HTTPS URI, make sure the server's certificate is valid. Message:  " + downloadEventArgs.Error.Message;
                    }

                    if (errorMessage != null)
                    {
                        SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(Protocol));
                        await SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);
                        completionSource.SetResult(null);
                    }
                };

                downloadClient.DownloadDataAsync(webURI);

                ScriptRunnerAgent selectedAgent = await completionSource.Task;

                if (selectedAgent != null)
                {
                    scriptRunner.Agent = selectedAgent;
                }
            };

            contentLayout.Children.Add(setAgentButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}