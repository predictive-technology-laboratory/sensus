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

using Sensus.Probes.User.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
			scriptRunnersList.ItemTemplate = new DataTemplate(typeof(DarkModeCompatibleTextCell));
			scriptRunnersList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(ScriptRunner.Caption));
			scriptRunnersList.ItemsSource = probe.ScriptRunners;
			scriptRunnersList.ItemTapped += async (o, e) =>
			{
				if (scriptRunnersList.SelectedItem == null)
				{
					return;
				}

				ScriptRunner selectedScriptRunner = scriptRunnersList.SelectedItem as ScriptRunner;
				int selectedIndex = probe.ScriptRunners.IndexOf(selectedScriptRunner);

				List<string> actions = new string[] { "Edit", "Copy", "Delete" }.ToList();

				if (selectedIndex < probe.ScriptRunners.Count - 1)
				{
					actions.Insert(0, "Move Down");
				}

				if (selectedIndex > 0)
				{
					actions.Insert(0, "Move Up");
				}

				string selectedAction = await DisplayActionSheet(selectedScriptRunner.Name, "Cancel", null, actions.ToArray());

				if (selectedAction == "Move Up")
				{
					probe.ScriptRunners.Move(selectedIndex, selectedIndex - 1);
				}
				else if (selectedAction == "Move Down")
				{
					probe.ScriptRunners.Move(selectedIndex, selectedIndex + 1);
				}
				else if (selectedAction == "Edit")
				{
					await Navigation.PushAsync(new ScriptRunnerPage(selectedScriptRunner));
				}
				else if (selectedAction == "Copy")
				{
					ScriptRunner copy = selectedScriptRunner.Copy();

					probe.ScriptRunners.Add(copy);
				}
				else if (selectedAction == "Delete")
				{
					if (await DisplayAlert("Delete " + selectedScriptRunner.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
					{
						await selectedScriptRunner.StopAsync();
						selectedScriptRunner.Enabled = false;
						selectedScriptRunner.Triggers.Clear();

						probe.ScriptRunners.Remove(selectedScriptRunner);

						foreach(ScriptRunner runner in probe.ScriptRunners)
						{
							if (runner.NextScript == selectedScriptRunner)
							{
								runner.NextScript = null;
							}
						}

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

		public void MakeNameUnique(ScriptRunner runner, ScriptProbe probe)
		{
			try
			{
				string pattern = @"\s*-\s*Copy\s*(?<number>\d*)$";

				runner.Name = Regex.Replace(runner.Name, pattern, "");

				string countString = probe.ScriptRunners.Max(x => Regex.Match(x.Name, $@"(?<={runner.Name}){pattern}").Groups["number"].Value);

				int.TryParse(countString, out int count);

				count = Math.Max(count, probe.ScriptRunners.Count(x => Regex.IsMatch(x.Name, $@"(?<={runner.Name})({pattern})?$")) - 1);

				runner.Name += $" - Copy {count + 1}";
			}
			catch
			{
				SensusServiceHelper.Get().Logger.Log("Error creating unique script runner name", LoggingLevel.Normal, GetType());
			}
		}
	}
}