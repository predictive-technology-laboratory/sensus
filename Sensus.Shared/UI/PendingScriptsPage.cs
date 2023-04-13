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
using Sensus.Probes.User.Scripts;
using Xamarin.Forms;
using System.Threading.Tasks;
using Sensus.Notifications;

namespace Sensus.UI
{
	public class PendingScriptsPage : RunScriptsPage
	{
		/// <summary>
		/// Enables tap-and-hold to remove a pending survey.
		/// </summary>
		private class PendingScriptTextCell : DarkModeCompatibleTextCell
		{
			public PendingScriptTextCell()
			{
				MenuItem deleteMenuItem = new MenuItem { Text = "Delete", IsDestructive = true };
				deleteMenuItem.SetBinding(MenuItem.CommandParameterProperty, ".");
				deleteMenuItem.Clicked += async (sender, e) =>
				{
					Script scriptToDelete = (sender as MenuItem).CommandParameter as Script;

					if (SensusServiceHelper.Get().RemoveScripts(scriptToDelete))
					{
						await SensusServiceHelper.Get().IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.Badge, scriptToDelete.Runner.Probe.Protocol);
					}

					// let the script agent know and store a datum to record the event
					await (scriptToDelete.Runner.Probe.Agent?.ObserveAsync(scriptToDelete, ScriptState.Deleted) ?? Task.CompletedTask);
					scriptToDelete.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Deleted, DateTimeOffset.UtcNow, scriptToDelete, scriptToDelete.Runner.SavedState?.SessionId), CancellationToken.None);
				};

				ContextActions.Add(deleteMenuItem);
			}
		}

		protected override void SetUpScriptList()
		{
			_scriptList.SetBinding(IsVisibleProperty, new Binding("Count", converter: new ViewVisibleValueConverter(), converterParameter: false));  // don't show list when there are no surveys
			_scriptList.ItemTemplate = new DataTemplate(typeof(PendingScriptTextCell));
			_scriptList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Script.Caption));
			_scriptList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(Script.SubCaption));
			_scriptList.ItemsSource = _scripts;
		}

		public PendingScriptsPage() : base(SensusServiceHelper.Get().ScriptsToRun, false)
		{
			Title = "Pending Surveys";

			//SetUpScriptList();

			// display an informative message when there are no surveys
			Label noSurveysLabel = new Label
			{
				Text = "You have no pending surveys.",
				TextColor = Color.Accent,
				FontSize = 20,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center,
				BindingContext = _scripts
			};

			noSurveysLabel.SetBinding(IsVisibleProperty, new Binding("Count", converter: new ViewVisibleValueConverter(), converterParameter: true));

			_contentGrid.Children.Add(noSurveysLabel, 0, 0);

			ToolbarItems.Add(new ToolbarItem("Clear", null, async () =>
			{
				if (await DisplayAlert("Clear all surveys?", "This action cannot be undone.", "Clear", "Cancel"))
				{
					if (await SensusServiceHelper.Get().ClearScriptsAsync())
					{
						await SensusServiceHelper.Get().IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.None, null);
					}
				}
			}));

			// use timer to update available surveys
			System.Timers.Timer filterTimer = new System.Timers.Timer(1000);

			filterTimer.Elapsed += async (sender, e) =>
			{
				if (await SensusServiceHelper.Get().RemoveExpiredScriptsAsync())
				{
					await SensusServiceHelper.Get().IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.Badge, null);
				}
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