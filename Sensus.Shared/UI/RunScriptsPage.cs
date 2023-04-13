using Sensus.Notifications;
using Sensus.Probes.User.Scripts;
using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
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
			return await SensusServiceHelper.Get().RunScriptAsync(script, _manualRun);
		}
	}
}
