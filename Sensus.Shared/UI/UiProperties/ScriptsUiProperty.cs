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
using Xamarin.Forms;

namespace Sensus.UI.UiProperties
{
	/// <summary>
	/// Decorated members should be rendered as a list of Scripts in the protocol.
	/// </summary>
	public class ScriptsUiProperty : UiProperty
	{
		private readonly string _scriptGroup;
		private StackLayout _view;
		private IList<ScriptRunner> _current;
		private bool _multiple;

		public ScriptsUiProperty(string labelText, bool editable, int order, bool required, string scriptGroup = null) : base(labelText, editable, order, required)
		{
			_scriptGroup = scriptGroup;
			_current = new List<ScriptRunner>();
		}

		private const string NEW_STRING = "New...";

		private View GetView(PropertyInfo property, object o, List<ScriptRunner> runners, int index, ScriptProbe probe)
		{
			Picker picker = new()
			{
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			foreach (ScriptRunner item in runners)
			{
				picker.Items.Add(item.Caption);
			}

			picker.Items.Add(NEW_STRING);

			picker.SelectedIndex = runners.IndexOf(_current.ElementAtOrDefault(index));

			picker.SelectedIndexChanged += async (s, e) =>
			{
				ScriptRunner selected = null;

				if (picker.SelectedIndex >= 0)
				{
					if (picker.SelectedIndex == runners.Count)
					{
						selected = new("New Script", probe)
						{
							ScriptGroup = _scriptGroup
						};

						await (Application.Current as App).DetailPage.Navigation.PushAsync(new ScriptRunnerPage(selected));

						runners.Add(selected);

						probe.ScriptRunners.Add(selected);

						picker.Items.Insert(picker.SelectedIndex, selected.Caption);
					}
					else
					{
						selected = runners[picker.SelectedIndex];
					}
				}

				if (_multiple)
				{
					if (_current.Count <= index)
					{
						index = _current.Count;

						_current.Add(selected);

						if (_view.Children.FirstOrDefault(x => x.IsVisible == false) is View hiddenView)
						{
							hiddenView.IsVisible = true;
						}
						else
						{
							_view.Children.Add(GetView(property, o, runners, index + 1, probe));
						}
					}
					else if (selected != null)
					{
						_current[index] = selected;
					}
					else
					{
						_current.RemoveAt(index);

						// possibly move the view ...
						View moved = _view.Children[index];

						_view.Children.Remove(moved);

						index = _current.Count;

						_view.Children.Add(moved);

						moved.IsVisible = false;
					}
				}
				else
				{
					_current[0] = selected;

					property.SetValue(o, selected);
				}
			};

			Button clearButton = new()
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

			StackLayout view = new()
			{
				Orientation = StackOrientation.Horizontal,
				Children = { picker, clearButton }
			};

			return view;
		}

		public override View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter)
		{
			bindingProperty = null;
			converter = null;

			List<ScriptRunner> runners = new();
			ScriptProbe probe = null;

			if (o is ScriptRunner scriptRunner)
			{
				probe = scriptRunner.Probe;

				runners = probe.ScriptRunners.Except(new[] { scriptRunner }).ToList();
			}
			else if (o is ScriptSchedulerInput scriptSchedulerInput)
			{
				probe = scriptSchedulerInput.Runner.Probe;

				runners = probe.ScriptRunners.ToList();
			}
			else if (o is Protocol protocol)
			{
				probe = protocol.Probes.OfType<ScriptProbe>().First();

				runners = probe.ScriptRunners.ToList();
			}

			if (string.IsNullOrWhiteSpace(_scriptGroup) == false)
			{
				runners = runners.Where(x => x.ScriptGroup == _scriptGroup).ToList();
			}

			_multiple = false;

			if (typeof(IList<ScriptRunner>).IsAssignableFrom(property.PropertyType))
			{
				_current = (IList<ScriptRunner>)property.GetValue(o);

				if (_current == null)
				{
					try
					{
						_current = (IList<ScriptRunner>)Activator.CreateInstance(property.PropertyType);
					}
					catch
					{
						_current = new List<ScriptRunner>();

						if (property.PropertyType.IsAssignableFrom(typeof(List<ScriptRunner>)))
						{
							property.SetValue(o, _current);
						}
					}
				}

				_multiple = true;
			}
			else if (property.PropertyType == typeof(ScriptRunner))
			{
				_current = new List<ScriptRunner>() { (ScriptRunner)property.GetValue(o) };
			}

			_view = new();

			for (int index = 0; index < _current.Count; index++)
			{
				_view.Children.Add(GetView(property, o, runners, index, probe));
			}

			if (_multiple)
			{
				_view.Children.Add(GetView(property, o, runners, _view.Children.Count + 1, probe));
			}

			return _view;
		}
	}
}
