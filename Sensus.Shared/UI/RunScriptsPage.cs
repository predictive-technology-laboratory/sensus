using Sensus.Notifications;
using Sensus.Probes.User.Scripts;
using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI
{
	public abstract class RunScriptsPage : ContentPage
	{
		protected abstract void SetUpScriptList();

		protected IEnumerable<Script> _scripts;
		protected Grid _contentGrid;
		protected ListView _scriptList;
		protected bool _manualRun;

		public RunScriptsPage(IEnumerable<Script> scripts, bool manualRun)
		{
			_scripts = scripts;
			_scriptList = new ListView(ListViewCachingStrategy.RecycleElement);
			_manualRun = manualRun;

			SetUpScriptList();

			_scriptList.ItemTapped += ItemTapped;

			// create grid showing surveys
			_contentGrid = new Grid
			{
				RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } },
				ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } },
				VerticalOptions = LayoutOptions.FillAndExpand
			};

			_contentGrid.Children.Add(_scriptList, 0, 0);

			Content = _contentGrid;
		}

		protected virtual async void ItemTapped(object sender, ItemTappedEventArgs args)
		{
			if (_scriptList.SelectedItem == null)
			{
				return;
			}

			Script selectedScript = _scriptList.SelectedItem as Script;

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
				_scriptList.SelectedItem = null;

				return;
			}

			await RunScriptAsync(selectedScript);

			// must reset the script selection manually
			_scriptList.SelectedItem = null;
		}

		protected virtual async Task<bool> RunScriptAsync(Script script)
		{
			bool submitted = false;

			script.Submitting = true;

			// let the script agent know and store a datum to record the event
			await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Opened) ?? Task.CompletedTask);
			script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Opened, DateTimeOffset.UtcNow, script), CancellationToken.None);

			IEnumerable<InputGroup> inputGroups = await SensusServiceHelper.Get().PromptForInputsAsync(script.RunTime, script.InputGroups, null, script.Runner.AllowCancel, null, null, script.Runner.IncompleteSubmissionConfirmation, "Are you ready to submit your responses?", script.Runner.DisplayProgress, null);

			bool userCancelled = inputGroups == null;

			// track script state and completions. do this immediately so that all timestamps are as accurate as possible.
			if (userCancelled)
			{
				// let the script agent know and store a datum to record the event
				await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Cancelled) ?? Task.CompletedTask);
				script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Cancelled, DateTimeOffset.UtcNow, script), CancellationToken.None);
			}
			else
			{
				// let the script agent know and store a datum to record the event
				await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Submitted) ?? Task.CompletedTask);
				script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Submitted, DateTimeOffset.UtcNow, script), CancellationToken.None);

				// track times when script is completely valid and wasn't cancelled by the user
				if (script.Valid)
				{
					// add completion time and remove all completion times before the participation horizon
					lock (script.Runner.CompletionTimes)
					{
						script.Runner.CompletionTimes.Add(DateTime.Now);
						script.Runner.CompletionTimes.RemoveAll(completionTime => completionTime < script.Runner.Probe.Protocol.ParticipationHorizon);
					}
				}
			}

			// process/store all inputs in the script
			bool inputStored = false;
			foreach (InputGroup inputGroup in script.InputGroups)
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
						await script.Runner.Probe.StoreDatumAsync(new ScriptDatum(input.CompletionTimestamp.GetValueOrDefault(DateTimeOffset.UtcNow),
																						  script.Runner.Script.Id,
																						  script.Runner.Name,
																						  input.GroupId,
																						  input.Id,
																						  script.Id,
                                                                                          input.LabelText,
                                                                                          input.Name,
																						  input.Value,
																						  script.CurrentDatum?.Id,
																						  input.Latitude,
																						  input.Longitude,
																						  input.LocationUpdateTimestamp,
																						  script.RunTime.Value,
																						  input.CompletionRecords,
																						  input.SubmissionTimestamp.Value,
																						  _manualRun), CancellationToken.None);

						inputStored = true;
					}
				}
			}

			// remove the submitted script. this should be done before the script is marked 
			// as not submitting in order to prevent the user from reopening it.
			if (!userCancelled)
			{
				if (SensusServiceHelper.Get().RemoveScripts(script))
				{
					await SensusServiceHelper.Get().IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.Badge, script.Runner.Probe.Protocol);
				}

				submitted = true;
			}

			// update UI to indicate that the script is no longer being submitted. this should 
			// be done after the script is removed in order to prevent the user from retaking the script.
			script.Submitting = false;

			// run a local-to-remote transfer if desired, respecting wifi requirements. do this after everything above, as it may take
			// quite some time to transfer the data depending on its size.
			if (inputStored && script.Runner.ForceRemoteStorageOnSurveySubmission)
			{
				SensusServiceHelper.Get().Logger.Log("Forcing a local-to-remote transfer.", LoggingLevel.Normal, typeof(Script));
				await script.Runner.Probe.Protocol.RemoteDataStore.WriteLocalDataStoreAsync(CancellationToken.None);
			}

			SensusServiceHelper.Get().Logger.Log("\"" + script.Runner.Name + "\" has completed processing.", LoggingLevel.Normal, typeof(Script));

			return submitted;
		}
	}
}
