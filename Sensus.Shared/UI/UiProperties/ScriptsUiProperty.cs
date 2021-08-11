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
		public ScriptsUiProperty(string labelText, bool editable, int order, bool required) : base(labelText, editable, order, required)
		{

		}

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

			if (o is ScriptRunner scriptRunner)
			{
				List<ScriptRunner> scripts = scriptRunner.Probe.ScriptRunners.Except(new[] { scriptRunner }).ToList();

				foreach (ScriptRunner item in scripts)
				{
					picker.Items.Add(item.Caption);
				}

				picker.SelectedIndex = scripts.IndexOf((ScriptRunner)property.GetValue(scriptRunner));

				picker.SelectedIndexChanged += (o, e) =>
				{
					if (picker.SelectedIndex >= 0)
					{
						property.SetValue(scriptRunner, scripts[picker.SelectedIndex]);
					}
					else
					{
						property.SetValue(scriptRunner, null);
					}
				};
			}

			return view;
		}
	}
}
