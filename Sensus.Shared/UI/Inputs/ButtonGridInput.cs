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

using Newtonsoft.Json;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ButtonGridInput : Input, IVariableDefiningInput
	{
		private string _definedVariable;
		private object _value;
		protected ButtonGridView _grid;

		private ButtonStates _defaultState;

		public ButtonGridInput() : base()
		{
			Buttons = new List<string>();
			ColumnCount = 1;

			_defaultState = ButtonStates.Default;
		}

		public override object Value => _value;

		public override bool Enabled
		{
			get
			{
				return _grid?.IsEnabled ?? false;
			}
			set
			{
				if (_grid != null)
				{
					_grid.IsEnabled = value;
				}
			}
		}

		public override string DefaultName => "Button Grid";

		[EntryStringUiProperty("Define Variable:", true, 2, false)]
		public string DefinedVariable
		{
			get
			{
				return _definedVariable;
			}
			set
			{
				_definedVariable = value?.Trim();
			}
		}

		[EditableListUiProperty("Buttons:", true, 2, true)]
		public List<string> Buttons { get; set; }

		[EntryIntegerUiProperty("Number of Columns:", true, 3, false)]
		public int ColumnCount { get; set; }

		[OnOffUiProperty("Selectable:", true, 4)]
		public bool Selectable { get; set; }

		[EntryIntegerUiProperty("Min. Number of Selections:", true, 4, false)]
		public int MinSelectionCount { get; set; }

		[EntryIntegerUiProperty("Max. Number of Selections:", true, 4, false)]
		public int MaxSelectionCount { get; set; }

		[OnOffUiProperty("Leave Incorrect Value:", true, 4)]
		public bool LeaveIncorrectValue { get; set; }

		[OnOffUiProperty("Split into Value/Text Pairs:", true, 4)]
		public bool SplitValueTextPairs { get; set; }

		[JsonIgnore]
		public List<ButtonWithValue> GridButtons => _grid?.Buttons.ToList() ?? new List<ButtonWithValue>();

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				if (Selectable)
				{
					_defaultState = ButtonStates.Selectable;
				}

				if (LeaveIncorrectValue == false)
				{
					DelayEnded += (s, e) =>
					{
						foreach (ButtonWithValue otherButton in _grid.Buttons)
						{
							if (otherButton.State == ButtonStates.Incorrect)
							{
								otherButton.State = ButtonStates.Default;
							}
						}
					};
				}

				int maxSelectionCount = Math.Min(MaxSelectionCount, Buttons.Count);
				int minSelectionCount = Math.Min(MinSelectionCount, maxSelectionCount);

				if (Required)
				{
					minSelectionCount = Math.Max(1, minSelectionCount);
				}

				_grid = new ButtonGridView(ColumnCount, (s, e) =>
				{
					ButtonWithValue button = (ButtonWithValue)s;

					if (Selectable && maxSelectionCount > 1)
					{
						List<ButtonWithValue> selectedButtons = _grid.Buttons.Where(x => x.State == ButtonStates.Selected).ToList();

						if (button.State == ButtonStates.Selectable)
						{
							if (selectedButtons.Count < maxSelectionCount)
							{
								selectedButtons.Add(button);

								button.State = ButtonStates.Selected;
							}
						}
						else if (button.State == ButtonStates.Selected)
						{
							if (selectedButtons.Count > minSelectionCount)
							{
								selectedButtons.Remove(button);

								button.State = ButtonStates.Selectable;
							}
						}

						_value = selectedButtons.Select(x => x.Value).ToArray();

						Complete = selectedButtons.Count >= minSelectionCount;
					}
					else
					{
						_value = button.Value;

						Complete = true;

						foreach (ButtonWithValue otherButton in _grid.Buttons)
						{
							otherButton.State = _defaultState;
						}

						if (CorrectValue != null)
						{
							if (Correct)
							{
								button.State = ButtonStates.Correct;
							}
							else
							{
								button.State = ButtonStates.Incorrect;
							}
						}
						else if (Selectable)
						{
							button.State = ButtonStates.Selected;
						}
					}
				});

				foreach (string buttonValue in Buttons)
				{
					string text = buttonValue;
					string value = buttonValue;

					if (SplitValueTextPairs)
					{
						string[] pair = Regex.Split(buttonValue, "(?<=[^:])::(?=[^:])")
							.Select(x => Regex.Replace(x, ":::", "::"))
							.ToArray();

						if (pair.Length > 2)
						{
							pair[1] = string.Join("::", pair.Skip(1));
						}

						value = pair.FirstOrDefault();
						text = pair.LastOrDefault();
					}

					ButtonWithValue button = _grid.AddButton(text, value);

					if (Selectable)
					{
						button.State = ButtonStates.Selectable;
					}
				}

				_grid.Arrange();

				base.SetView(_grid);
			}

			return base.GetView(index);
		}
	}
}
