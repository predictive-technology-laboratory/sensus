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
using System.Threading;
using System.Globalization;
using Sensus.Context;
using Sensus.UI.Inputs;
using Sensus.Probes.User.Scripts;
using Xamarin.Forms;
using System.Collections.Generic;

namespace Sensus.UI
{
    public class PendingScriptsPage : ContentPage
    {
        private class ViewVisibleValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == null)
                {
                    return false;
                }

                int count = (int)value;
                bool zeroMeansVisible = (bool)parameter;
                return (count == 0) == zeroMeansVisible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value;
            }
        }

        /// <summary>
        /// Enables tap-and-hold to remove a pending survey.
        /// </summary>
        private class PendingScriptTextCell : TextCell
        {
            public PendingScriptTextCell()
            {
                MenuItem deleteMenuItem = new MenuItem { Text = "Delete", IsDestructive = true };
                deleteMenuItem.SetBinding(MenuItem.CommandParameterProperty, ".");
                deleteMenuItem.Clicked += async (sender, e) =>
                {
                    Script scriptToDelete = (sender as MenuItem).CommandParameter as Script;

                    await SensusServiceHelper.Get().RemoveScriptAsync(scriptToDelete, true);

                    // let the script agent know and store a datum to record the event
                    scriptToDelete.Runner.Probe.Agent?.Observe(scriptToDelete, ScriptState.Deleted);
                    await scriptToDelete.Runner.Probe.StoreDatumAsync(new ScriptStateDatum(ScriptState.Deleted, DateTimeOffset.UtcNow, scriptToDelete), default(CancellationToken));
                };

                ContextActions.Add(deleteMenuItem);
            }
        }

        public PendingScriptsPage()
        {
            Title = "Pending Surveys";

            ListView scriptList = new ListView(ListViewCachingStrategy.RecycleElement)
            {                
                BindingContext = SensusServiceHelper.Get().ScriptsToRun  // used to show/hide when there are no surveys
            };

            scriptList.SetBinding(IsVisibleProperty, new Binding("Count", converter: new ViewVisibleValueConverter(), converterParameter: false));  // don't show list when there are no surveys
            scriptList.ItemTemplate = new DataTemplate(typeof(PendingScriptTextCell));
            scriptList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Script.Caption));
            scriptList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(Script.SubCaption));
            scriptList.ItemsSource = SensusServiceHelper.Get().ScriptsToRun;
            scriptList.ItemTapped += async (o, e) =>
            {
                if (scriptList.SelectedItem == null)
                {
                    return;
                }

                Script selectedScript = scriptList.SelectedItem as Script;

                // the script might be saved from a previous run of the app, and the protocol might not yet be running.
                if (selectedScript.Runner.Probe.Protocol.State == ProtocolState.Starting)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("The study associated with this survey is currently starting up. Please try again shortly or check the Studies page.");
                    return;
                }
                else if (selectedScript.Runner.Probe.Protocol.State == ProtocolState.Stopping)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("You cannot take this survey because the associated study is currently shutting down.");
                    return;
                }
                else if (selectedScript.Runner.Probe.Protocol.State == ProtocolState.Stopped)
                {
                    // ask the user to start the protocol associated with the script, if it is not already running.
                    if (await DisplayAlert("Start Study?", "The study associated with this survey is not running. You cannot take this survey unless you start the study. Would you like to start the study now?", "Yes", "No"))
                    {
                        await selectedScript.Runner.Probe.Protocol.StartWithUserAgreementAsync();

                        // if the protocol failed to start, or the user cancelled the start, then bail.
                        if (selectedScript.Runner.Probe.Protocol.State != ProtocolState.Running)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Study was not started.");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                // let the script agent know and store a datum to record the event
                selectedScript.Runner.Probe.Agent?.Observe(selectedScript, ScriptState.Opened);
                await selectedScript.Runner.Probe.StoreDatumAsync(new ScriptStateDatum(ScriptState.Opened, DateTimeOffset.UtcNow, selectedScript), default(CancellationToken));

                selectedScript.Submitting = true;

                IEnumerable<InputGroup> inputGroups = await SensusServiceHelper.Get().PromptForInputsAsync(selectedScript.RunTime, selectedScript.InputGroups, null, selectedScript.Runner.AllowCancel, null, null, selectedScript.Runner.IncompleteSubmissionConfirmation, "Are you ready to submit your responses?", selectedScript.Runner.DisplayProgress, null);

                bool canceled = inputGroups == null;

                if (canceled)
                {
                    // let the script agent know and store a datum to record the event
                    selectedScript.Runner.Probe.Agent?.Observe(selectedScript, ScriptState.Cancelled);
                    await selectedScript.Runner.Probe.StoreDatumAsync(new ScriptStateDatum(ScriptState.Cancelled, DateTimeOffset.UtcNow, selectedScript), default(CancellationToken));
                }
                else
                {
                    // let the script agent know and store a datum to record the event
                    selectedScript.Runner.Probe.Agent?.Observe(selectedScript, ScriptState.Submitted);
                    await selectedScript.Runner.Probe.StoreDatumAsync(new ScriptStateDatum(ScriptState.Submitted, DateTimeOffset.UtcNow, selectedScript), default(CancellationToken));

                    // track times when script is completely valid and wasn't cancelled by the user
                    if (selectedScript.Valid)
                    {
                        // add completion time and remove all completion times before the participation horizon
                        lock (selectedScript.Runner.CompletionTimes)
                        {
                            selectedScript.Runner.CompletionTimes.Add(DateTime.Now);
                            selectedScript.Runner.CompletionTimes.RemoveAll(completionTime => completionTime < selectedScript.Runner.Probe.Protocol.ParticipationHorizon);
                        }
                    }
                }

                // process/store all inputs in the script
                bool inputStored = false;
                foreach (InputGroup inputGroup in selectedScript.InputGroups)
                {
                    foreach (Input input in inputGroup.Inputs)
                    {
                        // only consider inputs that still need to be stored. if an input has already been stored, it should be ignored.
                        if (input.NeedsToBeStored)
                        {
                            // if the user canceled the prompts, reset the input. we reset here within the above if-check because if an
                            // input has already been stored we should not reset it. its value and read-only status are fixed for all 
                            // time, even if the prompts are later redisplayed.
                            if (canceled)
                            {
                                input.Reset();
                            }
                            else if (input.Valid && input.Display)  // store all inputs that are valid and displayed. some might be valid from previous responses but not displayed because the user navigated back through the survey and changed a previous response that caused a subsesequently displayed input to be hidden via display contingencies.
                            {
                                // the _script.Id allows us to link the data to the script that the user created. it never changes. on the other hand, the script
                                // that is passed into this method is always a copy of the user-created script. the script.Id allows us to link the various data
                                // collected from the user into a single logical response. each run of the script has its own script.Id so that responses can be
                                // grouped across runs. this is the difference between scriptId and runId in the following line.
                                await selectedScript.Runner.Probe.StoreDatumAsync(new ScriptDatum(input.CompletionTimestamp.GetValueOrDefault(DateTimeOffset.UtcNow), selectedScript.Runner.Script.Id, selectedScript.Runner.Name, input.GroupId, input.Id, selectedScript.Id, input.Value, selectedScript.CurrentDatum?.Id, input.Latitude, input.Longitude, input.LocationUpdateTimestamp, selectedScript.RunTime.Value, input.CompletionRecords, input.SubmissionTimestamp.Value), default(CancellationToken));

                                inputStored = true;

                                // once inputs are stored, they should not be stored again, nor should the user be able to modify them if the script is viewed again.
                                input.NeedsToBeStored = false;
                                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() => input.Enabled = false);
                            }
                        }
                    }
                }

                // update UI to indicate that the script is no longer being submitted
                selectedScript.Submitting = false;

                // if the user cancelled, deselect the survey.
                if (canceled)
                {
                    scriptList.SelectedItem = null;
                }
                else
                {
                    // otherwise, remove the submitted script.
                    await SensusServiceHelper.Get().RemoveScriptAsync(selectedScript, true);
                }

                SensusServiceHelper.Get().Logger.Log("\"" + selectedScript.Runner.Name + "\" has finished running.", LoggingLevel.Normal, typeof(Script));

                // run a local-to-remote transfer if desired, respecting wifi requirements. do this after everything above, as it may take
                // quite some time to transfer the data depending on its size.
                if (inputStored && selectedScript.Runner.ForceRemoteStorageOnSurveySubmission)
                {
                    SensusServiceHelper.Get().Logger.Log("Forcing a local-to-remote transfer.", LoggingLevel.Normal, typeof(Script));
                    await selectedScript.Runner.Probe.Protocol.RemoteDataStore.WriteLocalDataStoreAsync(CancellationToken.None);
                }
            };

            // display an informative message when there are no surveys
            Label noSurveysLabel = new Label
            {
                Text = "You have no pending surveys.",
                TextColor = Color.Accent,
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                BindingContext = SensusServiceHelper.Get().ScriptsToRun
            };

            noSurveysLabel.SetBinding(IsVisibleProperty, new Binding("Count", converter: new ViewVisibleValueConverter(), converterParameter: true));

            // create grid showing surveys
            Grid contentGrid = new Grid
            {
                RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } },
                ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } },
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            contentGrid.Children.Add(noSurveysLabel, 0, 0);
            contentGrid.Children.Add(scriptList, 0, 0);

            Content = contentGrid;

            ToolbarItems.Add(new ToolbarItem("Clear", null, async () =>
            {
                if (await DisplayAlert("Clear all surveys?", "This action cannot be undone.", "Clear", "Cancel"))
                {
                    await SensusServiceHelper.Get().ClearScriptsAsync();
                }
            }));

            // use timer to update available surveys
            System.Timers.Timer filterTimer = new System.Timers.Timer(1000);

            filterTimer.Elapsed += async (sender, e) =>
            {
                await SensusServiceHelper.Get().RemoveExpiredScriptsAsync(true);
            };

            Appearing += (sender, e) =>
            {
                filterTimer.Start();
            };

            Disappearing += (sender, e) =>
            {
                filterTimer.Stop();
            };
        }
    }
}