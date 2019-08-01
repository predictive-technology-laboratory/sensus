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
using Xamarin.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using Sensus.UI.Inputs;

namespace Sensus.UI
{
	public class UserInitiatedScriptsPage : RunScriptsPage
	{
		protected override void SetUpScriptList()
		{
			_scriptList.ItemTemplate = new DataTemplate(typeof(TextCell));

			_scriptList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Script.Caption));
			_scriptList.ItemsSource = new ObservableCollection<Script>(_scripts);

		}

		public static IEnumerable<Script> GetProtocolScripts(Protocol protocol)
		{
			return protocol.Probes.OfType<ScriptProbe>()
				.SelectMany(x => x.ScriptRunners)
				.Where(x => x.Enabled && x.AllowUserInitiation && x.Script.InputGroups.SelectMany(y => y.Inputs).Any())
				.Select(x => x.Script);
		}

		public UserInitiatedScriptsPage(Protocol protocol) : base(GetProtocolScripts(protocol), true)
		{
			Title = $"{protocol.Name} Surveys";

			ToolbarItems.Add(new ToolbarItem("Take All", null, async () =>
			{
				if (await DisplayAlert("Take all surveys?", "Are you sure you want to take all surveys?", "Yes", "No"))
				{
					if (_scriptList.ItemsSource is ObservableCollection<Script> scripts)
					{
						foreach (Script script in scripts)
						{
							await RunScriptAsync(script);
						}
					}
				}
			}));
		}

		protected override async Task<bool> RunScriptAsync(Script script)
		{
			script.RunTime = DateTimeOffset.UtcNow;

			if (await base.RunScriptAsync(script))
			{
				foreach (Input input in script.InputGroups.SelectMany(x => x.Inputs))
				{
					input.Reset();
				}

				return true;
			}

			return false;
		}
	}
}
