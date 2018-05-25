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
                deleteMenuItem.Clicked += (sender, e) =>
                {
                    SensusServiceHelper.Get().RemoveScript((sender as MenuItem).CommandParameter as Script);
                };

                ContextActions.Add(deleteMenuItem);
            }
        }

        public PendingScriptsPage()
        {
            Title = "Pending Surveys";

            ListView scriptList = new ListView(ListViewCachingStrategy.RecycleElement)
            {
                BindingContext = SensusServiceHelper.Get().ScriptsToRun  // used to show/hid when there are no surveys
            };

            scriptList.SetBinding(IsVisibleProperty, new Binding("Count", converter: new ViewVisibleValueConverter(), converterParameter: false));  // don't show list when there are no surveys
            scriptList.ItemTemplate = new DataTemplate(typeof(PendingScriptTextCell));
            scriptList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Script.Caption));
            scriptList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(Script.SubCaption));
            scriptList.ItemsSource = SensusServiceHelper.Get().ScriptsToRun;
            scriptList.ItemTapped += (o, e) =>
            {
                if (scriptList.SelectedItem == null)
                {
                    return;
                }

                Script selectedScript = scriptList.SelectedItem as Script;

                selectedScript.Submitting = true;

                SensusServiceHelper.Get().PromptForInputsAsync(selectedScript.RunTime,
                                                               selectedScript.InputGroups,
                                                               null,
                                                               selectedScript.Runner.AllowCancel,
                                                               null,
                                                               null,
                                                               selectedScript.Runner.IncompleteSubmissionConfirmation,
                                                               "Are you ready to submit your responses?",
                                                               selectedScript.Runner.DisplayProgress,
                                                               null,
                                                               inputGroups =>
                                                               {
                                                                   bool canceled = inputGroups == null;

                                                                   // process all inputs in the script
                                                                   foreach (InputGroup inputGroup in selectedScript.InputGroups)
                                                                   {
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
                                                                                   selectedScript.Runner.Probe.StoreDatum(new ScriptDatum(input.CompletionTimestamp.GetValueOrDefault(DateTimeOffset.UtcNow), selectedScript.Runner.Script.Id, selectedScript.Runner.Name, input.GroupId, input.Id, selectedScript.Id, input.Value, selectedScript.CurrentDatum?.Id, input.Latitude, input.Longitude, input.LocationUpdateTimestamp, selectedScript.RunTime.Value, input.CompletionRecords, input.SubmissionTimestamp.Value), default(CancellationToken));

                                                                                   // once inputs are stored, they should not be stored again, nor should the user be able to modify them if the script is viewed again.
                                                                                   input.NeedsToBeStored = false;
                                                                                   SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() => input.Enabled = false);
                                                                               }
                                                                           }
                                                                       }
                                                                   }

                                                                   if (selectedScript.Valid)
                                                                   {
                                                                       // add completion time and remove all completion times before the participation horizon
                                                                       lock (selectedScript.Runner.CompletionTimes)
                                                                       {
                                                                           selectedScript.Runner.CompletionTimes.Add(DateTime.Now);
                                                                           selectedScript.Runner.CompletionTimes.RemoveAll(completionTime => completionTime < selectedScript.Runner.Probe.Protocol.ParticipationHorizon);
                                                                       }
                                                                   }

                                                                   selectedScript.Submitting = false;

                                                                   if (!canceled)
                                                                   {
                                                                       SensusServiceHelper.Get().RemoveScript(selectedScript);
                                                                   }

                                                                   SensusServiceHelper.Get().Logger.Log("\"" + selectedScript.Runner.Name + "\" has finished running.", LoggingLevel.Normal, typeof(Script));
                                                               });
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
                if (await DisplayAlert("Clear surveys?", "This action cannot be undone.", "Clear", "Cancel"))
                {
                    SensusServiceHelper.Get().ClearScripts();
                }
            }));

            // use timer to update available surveys
            System.Timers.Timer filterTimer = new System.Timers.Timer(1000);

            filterTimer.Elapsed += (sender, e) =>
            {
                SensusServiceHelper.Get().RemoveExpiredScripts(true);
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