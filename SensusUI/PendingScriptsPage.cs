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
using SensusService;
using SensusService.Probes.User.Scripts;
using SensusUI.Inputs;
using Xamarin.Forms;
using System.Linq;

namespace SensusUI
{
    public class PendingScriptsPage : ContentPage
    {
        public PendingScriptsPage()
        {
            Title = "Pending Surveys";

            ListView scriptList = new ListView();
            scriptList.ItemTemplate = new DataTemplate(typeof(TextCell));
            scriptList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".Runner.Name"));
            scriptList.ItemsSource = SensusServiceHelper.Get().ScriptsToRun;
            scriptList.ItemTapped += (o, e) =>
            {
                if (scriptList.SelectedItem == null)
                    return;

                Script script = scriptList.SelectedItem as Script;

                // reset list selection
                scriptList.SelectedItem = null;

                SensusServiceHelper.Get().PromptForInputsAsync(script.RunTimestamp, script.InputGroups, null, script.Runner.AllowCancel, null, null, "You have not completed all required fields. You will not receive credit for your responses if you continue. Do you want to continue?", "Are you ready to submit your responses?", script.Runner.DisplayProgress, null, async inputGroups =>
                {
                    bool canceled = inputGroups == null;

                    // process all inputs in the script
                    foreach (InputGroup inputGroup in script.InputGroups)
                        foreach (Input input in inputGroup.Inputs)
                        {
                            // only consider inputs that still need to be stored. if an input has already been stored, it should be ignored.
                            if (input.NeedsToBeStored)
                            {
                                // if the user canceled the prompts, reset the input. we reset here within the above if-check because if an
                                // input has already been stored we should not reset it. its value and read-only status are fixed for all 
                                // time, even if the prompts are later redisplayed by the invalid script handler.
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
                                    await script.Runner.Probe.StoreDatumAsync(new ScriptDatum(input.CompletionTimestamp.GetValueOrDefault(DateTimeOffset.UtcNow), script.Runner.Script.Id, script.Runner.Name, input.GroupId, input.Id, script.Id, input.Value, script.CurrentDatum?.Id, input.Latitude, input.Longitude, input.LocationUpdateTimestamp, script.RunTimestamp.Value, input.CompletionRecords), default(CancellationToken));

                                    // once inputs are stored, they should not be stored again, nor should the user be able to modify them if the script is viewed again.
                                    input.NeedsToBeStored = false;
                                    Device.BeginInvokeOnMainThread(() => input.Enabled = false);
                                }
                            }
                        }

                    if (script.Valid)
                    {
                        // add completion time and remove all completion times before the participation horizon
                        lock (script.Runner.CompletionTimes)
                        {
                            script.Runner.CompletionTimes.Add(DateTime.Now);
                            script.Runner.CompletionTimes.RemoveAll(completionTime => completionTime < script.Runner.Probe.Protocol.ParticipationHorizon);
                        }
                    }

                    if (!canceled)
                    {
                        lock (SensusServiceHelper.Get().ScriptsToRun)
                        {
                            SensusServiceHelper.Get().ScriptsToRun.Remove(script);
                        }
                    }

                    SensusServiceHelper.Get().Logger.Log("\"" + script.Runner.Name + "\" has finished running.", LoggingLevel.Normal, typeof(Script));
                });
            };

            Content = scriptList;
        }
    }
}