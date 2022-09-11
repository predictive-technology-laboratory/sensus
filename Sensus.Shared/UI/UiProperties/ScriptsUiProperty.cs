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
using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI.UiProperties
{
	/// <summary>
	/// Decorated members should be rendered as a list of Scripts in the protocol.
	/// </summary>
	public class ScriptsUiProperty : UiProperty
	{
		private string _scriptGroup;

		public ScriptsUiProperty(string labelText, bool editable, int order, bool required, string scriptGroup = null) : base(labelText, editable, order, required)
		{
			_scriptGroup = scriptGroup;
		}

		private const string NEW_STRING = "New...";

		public override View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter)
		{
			Picker picker = new Picker
			{
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			Button clearButton = new Button
			{
				Padding = new Thickness(0),
				FontSize = 30,
				Text = "\x00D7"
			};

			clearButton.SizeChanged += (s, e) =>
			{
				clearButton.WidthRequest = clearButton.Height;
			};

			clearButton.Clicked += (s, e) =>
			{
				picker.SelectedIndex = -1;
			};

			StackLayout view = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				Children = { picker, clearButton }
			};

			bindingProperty = null;
			converter = null;

			List<ScriptRunner> scripts = new List<ScriptRunner>();
			//ScriptRunner runner = null;
			ScriptProbe probe = null;

			if (o is ScriptRunner scriptRunner)
			{
				//runner = scriptRunner;
				probe = scriptRunner.Probe;
				scripts = probe.ScriptRunners.Except(new[] { scriptRunner }).ToList();
			}
			else if (o is ScriptSchedulerInput scriptSchedulerInput)
			{
				//runner = scriptSchedulerInput.Runner;
				probe = scriptSchedulerInput.Runner.Probe;
				scripts = probe.ScriptRunners.ToList(); //.Except(new[] { scriptSchedulerInput.Runner }).ToList();
			}
			else if (o is Protocol protocol)
			{
				probe = protocol.Probes.OfType<ScriptProbe>().First();
				scripts = probe.ScriptRunners.ToList();
			}

			if (string.IsNullOrWhiteSpace(_scriptGroup) == false)
			{
				scripts = scripts.Where(x => x.ScriptGroup == _scriptGroup).ToList();
			}

			foreach (ScriptRunner item in scripts)
			{
				picker.Items.Add(item.Caption);
			}

			picker.Items.Add(NEW_STRING);

			picker.SelectedIndex = scripts.IndexOf((ScriptRunner)property.GetValue(o));

			if (probe != null)
			{
				picker.SelectedIndexChanged += async (s, e) =>
				{
					if (picker.SelectedIndex >= 0)
					{
						if (picker.SelectedIndex == scripts.Count)
						{
							ScriptRunner newScriptRunner = new ScriptRunner("New Script", probe)
							{
								ScriptGroup = _scriptGroup
							};

							await (Application.Current as App).DetailPage.Navigation.PushAsync(new ScriptRunnerPage(newScriptRunner));

							scripts.Add(newScriptRunner);

							probe.ScriptRunners.Add(newScriptRunner);

							property.SetValue(o, newScriptRunner);

							picker.Items.Insert(picker.SelectedIndex, newScriptRunner.Caption);
						}
						else
						{
							property.SetValue(o, scripts[picker.SelectedIndex]);
						}
					}
					else
					{
						property.SetValue(o, null);
					}
				};
			}

			return view;
		}
	}
}
