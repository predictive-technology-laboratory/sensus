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
using System.Threading.Tasks;
using Sensus.Notifications;
using System.Linq;
using System.Collections.ObjectModel;

namespace Sensus.UI
{
	public class UserInitiatedScriptsPage : ContentPage
	{
		public UserInitiatedScriptsPage(Protocol protocol)
		{
			Title = $"{protocol.Name} Surveys";

			ListView scriptList = new ListView(ListViewCachingStrategy.RecycleElement);

			scriptList.ItemTemplate = new DataTemplate(typeof(TextCell));

			scriptList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Script.Caption));
			//scriptList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(Script.SubCaption));
			scriptList.ItemsSource = new ObservableCollection<Script>(protocol.Probes.OfType<ScriptProbe>().SelectMany(x => x.ScriptRunners).Where(x => x.Enabled && x.AllowUserInitiation).Select(x => x.Script)); // scripts; // SensusServiceHelper.Get().ScriptsToRun;
			scriptList.ItemTapped += async (o, e) =>
			{
				if (scriptList.SelectedItem == null)
				{
					return;
				}

				Script selectedScript = scriptList.SelectedItem as Script;

				bool abortScript = false;

				// the selected script might already be in the process of submission (e.g., waiting for GPS tagging). don't let the user open it again.
				if (selectedScript.Submitting)
				{
					await SensusServiceHelper.Get().FlashNotificationAsync("The selected survey has already been completed and is being submitted. You cannot take it again.");
					abortScript = true;
				}
				// the script might be saved from a previous run of the app, and the protocol might not yet be running.
				else if (selectedScript.Runner.Probe.Protocol.State == ProtocolState.Starting)
				{
					await SensusServiceHelper.Get().FlashNotificationAsync("The study associated with this survey is currently starting up. Please try again shortly or check the Studies page.");
					abortScript = true;
				}
				else if (selectedScript.Runner.Probe.Protocol.State == ProtocolState.Paused)
				{
					// ask the user to resume the protocol associated with the script
					if (await DisplayAlert("Resume Study?", "The study associated with this survey is paused. You cannot take this survey unless you resume the study. Would you like to resume the study now?", "Yes", "No"))
					{
						await selectedScript.Runner.Probe.Protocol.ResumeAsync();

						// if the protocol failed to resume, then bail.
						if (selectedScript.Runner.Probe.Protocol.State != ProtocolState.Running)
						{
							await SensusServiceHelper.Get().FlashNotificationAsync("Study was not resumed.");
							abortScript = true;
						}
					}
					else
					{
						abortScript = true;
					}
				}
				else if (selectedScript.Runner.Probe.Protocol.State == ProtocolState.Stopping)
				{
					await SensusServiceHelper.Get().FlashNotificationAsync("You cannot take this survey because the associated study is currently shutting down.");
					abortScript = true;
				}
				else if (selectedScript.Runner.Probe.Protocol.State == ProtocolState.Stopped)
				{
					// ask the user to start the protocol associated with the script
					if (await DisplayAlert("Start Study?", "The study associated with this survey is not running. You cannot take this survey unless you start the study. Would you like to start the study now?", "Yes", "No"))
					{
						await selectedScript.Runner.Probe.Protocol.StartWithUserAgreementAsync();

						// if the protocol failed to start, or the user cancelled the start, then bail.
						if (selectedScript.Runner.Probe.Protocol.State != ProtocolState.Running)
						{
							await SensusServiceHelper.Get().FlashNotificationAsync("Study was not started.");
							abortScript = true;
						}
					}
					else
					{
						abortScript = true;
					}
				}

				if (abortScript)
				{
					// must reset the script selection manually
					scriptList.SelectedItem = null;
					return;
				}

				selectedScript.Submitting = true;

				// let the script agent know and store a datum to record the event
				await (selectedScript.Runner.Probe.Agent?.ObserveAsync(selectedScript, ScriptState.Opened) ?? Task.CompletedTask);
				selectedScript.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Opened, DateTimeOffset.UtcNow, selectedScript), CancellationToken.None);

				IEnumerable<InputGroup> inputGroups = await SensusServiceHelper.Get().PromptForInputsAsync(selectedScript.RunTime, selectedScript.InputGroups, null, selectedScript.Runner.AllowCancel, null, null, selectedScript.Runner.IncompleteSubmissionConfirmation, "Are you ready to submit your responses?", selectedScript.Runner.DisplayProgress, null);

				// must reset the script selection manually
				scriptList.SelectedItem = null;

				bool userCancelled = inputGroups == null;

				// track script state and completions. do this immediately so that all timestamps are as accurate as possible.
				if (userCancelled)
				{
					// let the script agent know and store a datum to record the event
					await (selectedScript.Runner.Probe.Agent?.ObserveAsync(selectedScript, ScriptState.Cancelled) ?? Task.CompletedTask);
					selectedScript.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Cancelled, DateTimeOffset.UtcNow, selectedScript), CancellationToken.None);
				}
				else
				{
					// let the script agent know and store a datum to record the event
					await (selectedScript.Runner.Probe.Agent?.ObserveAsync(selectedScript, ScriptState.Submitted) ?? Task.CompletedTask);
					selectedScript.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Submitted, DateTimeOffset.UtcNow, selectedScript), CancellationToken.None);

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
						if (userCancelled)
						{
							input.Reset();
						}
						else if (input.Store)
						{
							// the _script.Id allows us to link the data to the script that the user created. it never changes. on the other hand, the script
							// that is passed into this method is always a copy of the user-created script. the script.Id allows us to link the various data
							// collected from the user into a single logical response. each run of the script has its own script.Id so that responses can be
							// grouped across runs. this is the difference between scriptId and runId in the following line.
							await selectedScript.Runner.Probe.StoreDatumAsync(new ScriptDatum(input.CompletionTimestamp.GetValueOrDefault(DateTimeOffset.UtcNow),
																							  selectedScript.Runner.Script.Id,
																							  selectedScript.Runner.Name,
																							  input.GroupId,
																							  input.Id,
																							  selectedScript.Id,
																							  input.Value,
																							  selectedScript.CurrentDatum?.Id,
																							  input.Latitude,
																							  input.Longitude,
																							  input.LocationUpdateTimestamp,
																							  selectedScript.RunTime.Value,
																							  input.CompletionRecords,
																							  input.SubmissionTimestamp.Value), CancellationToken.None);

							inputStored = true;
						}
					}
				}

				// remove the submitted script. this should be done before the script is marked 
				// as not submitting in order to prevent the user from reopening it.
				if (!userCancelled)
				{
					if (SensusServiceHelper.Get().RemoveScripts(selectedScript))
					{
						await SensusServiceHelper.Get().IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.Badge, selectedScript.Runner.Probe.Protocol);
					}
				}

				// update UI to indicate that the script is no longer being submitted. this should 
				// be done after the script is removed in order to prevent the user from retaking the script.
				selectedScript.Submitting = false;

				// run a local-to-remote transfer if desired, respecting wifi requirements. do this after everything above, as it may take
				// quite some time to transfer the data depending on its size.
				if (inputStored && selectedScript.Runner.ForceRemoteStorageOnSurveySubmission)
				{
					SensusServiceHelper.Get().Logger.Log("Forcing a local-to-remote transfer.", LoggingLevel.Normal, typeof(Script));
					await selectedScript.Runner.Probe.Protocol.RemoteDataStore.WriteLocalDataStoreAsync(CancellationToken.None);
				}

				SensusServiceHelper.Get().Logger.Log("\"" + selectedScript.Runner.Name + "\" has completed processing.", LoggingLevel.Normal, typeof(Script));
			};

			// create grid showing surveys
			Grid contentGrid = new Grid
			{
				RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } },
				ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } },
				VerticalOptions = LayoutOptions.FillAndExpand
			};

			contentGrid.Children.Add(scriptList, 0, 0);

			Content = contentGrid;
		}
	}
}
