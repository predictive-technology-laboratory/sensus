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
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ButtonGridInput : Input, IVariableDefiningInput
	{
		private string _definedVariable;
		private object _value;
		private string _otherResponseValue;

		protected ButtonGridView _grid;

		private ButtonStates _defaultState;

		public ButtonGridInput() : base()
		{
			Buttons = new List<string>();
			ColumnCount = 1;
			MaxSelectionCount = 1;

			_defaultState = ButtonStates.Default;
		}

		public override object Value
		{
			get
			{
				if (string.IsNullOrEmpty(_otherResponseValue) == false)
				{
					if (_value is string)
					{
						_value = new string[] { _value as string, _otherResponseValue };
					}
					else if (_value is string[] array)
					{
						_value = array.Union(new[] { _otherResponseValue }).ToArray();
					}
				}

				return _value;
			}
		}

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

		[EditableListUiProperty("\"Other\" Values:", true, 4, true)]
		public List<string> OtherValues { get; set; }

		[EntryStringUiProperty("\"Other\" Response Label:", true, 4, true)]
		public string OtherResponseLabel { get; set; }

		[EditableListUiProperty("Exclusive Values:", true, 4, true)]
		public List<string> ExclusiveValues { get; set; }

		[OnOffUiProperty("Auto size buttons:", true, 4)]
		public bool AutoSizeButtons { get; set; }

		[OnOffUiProperty("Show Long Text:", true, 4)]
		public bool ShowLongText { get; set; }

		private const int LONG_TEXT_LENGTH = 30;

		[JsonIgnore]
		public List<ButtonWithValue> GridButtons => _grid?.Buttons.ToList() ?? new List<ButtonWithValue>();

		private async Task<bool> HandleLongText(ButtonWithValue button)
		{
			Page page = Application.Current.MainPage.Navigation?.NavigationStack?.LastOrDefault() ?? Application.Current.MainPage;

			return button.Text.Length < LONG_TEXT_LENGTH || ShowLongText == false || await page.DisplayAlert("Do you want to select:", button.Text, "Yes", "No");
		}

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

				int maxSelectionCount = Math.Max(1, Math.Min(MaxSelectionCount, Buttons.Count));
				int minSelectionCount = Math.Min(MinSelectionCount, maxSelectionCount);

				if (Required)
				{
					minSelectionCount = Math.Max(1, minSelectionCount);
				}

				HashSet<string> otherValues = OtherValues?.ToHashSet() ?? new HashSet<string>();
				HashSet<string> exclusiveValues = ExclusiveValues?.ToHashSet() ?? new HashSet<string>();

				StackLayout otherLayout = null;
				Label otherLabel = null;
				Editor otherEditor = null;

				bool completeFunc(int selectedButtonCount, bool otherSelected) => Selectable == false || (selectedButtonCount >= minSelectionCount && (otherSelected == false || string.IsNullOrWhiteSpace(_otherResponseValue) == false));

				_grid = new ButtonGridView(ColumnCount, async (s, e) =>
				{
					ButtonWithValue button = (ButtonWithValue)s;
					List<ButtonWithValue> selectedButtons = _grid.Buttons.Where(x => x.State == ButtonStates.Selected).ToList();

					if (Selectable)
					{
						bool isExclusive = exclusiveValues.Contains(button.Value);

						if (button.State == ButtonStates.Selectable)
						{
							if (await HandleLongText(button))
							{
								if (isExclusive || maxSelectionCount == 1)
								{
									foreach (ButtonWithValue otherButton in _grid.Buttons)
									{
										otherButton.State = _defaultState;

										selectedButtons.Remove(otherButton);
									}
								}
								else
								{
									foreach (ButtonWithValue otherButton in selectedButtons.Where(x => exclusiveValues.Contains(x.Value)).ToList())
									{
										otherButton.State = _defaultState;

										selectedButtons.Remove(otherButton);
									}
								}

								if (selectedButtons.Count < maxSelectionCount)
								{
									selectedButtons.Add(button);

									button.State = ButtonStates.Selected;
								}
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
					}
					else
					{
						_value = button.Value;

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
						/*else if (Selectable)
						{
							if (selectedButtons.Contains(button))
							{
								if (selectedButtons.Count > minSelectionCount)
								{
									selectedButtons.Remove(button);

									button.State = ButtonStates.Selectable;
								}
							}
							else if (await HandleLongText(button))
							{
								selectedButtons.Add(button);

								button.State = ButtonStates.Selected;
							}
						}*/
					}

					bool otherSelected = false;

					if (Selectable && otherValues.Any())
					{
						IEnumerable<ButtonWithValue> selectedOtherButtons = selectedButtons.Where(x => otherValues.Contains(x.Value));

						otherSelected = selectedOtherButtons.Any();

						if (otherSelected)
						{
							_otherResponseValue = otherEditor.Text;

							if (string.IsNullOrWhiteSpace(OtherResponseLabel) && otherLabel != null)
							{
								if (selectedOtherButtons.Count() == 1)
								{
									otherLabel.Text = selectedOtherButtons.Single().Text + ":";

									if (otherLabel.Text.EndsWith("::"))
									{
										otherLabel.Text = otherLabel.Text[0..^1];
									}
								}
								else
								{
									otherLabel.Text = "Other:";
								}
							}
						}
						else
						{
							_otherResponseValue = null;
						}

						otherLayout.IsVisible = otherSelected;
					}

					Complete = completeFunc(selectedButtons.Count, otherSelected);
				})
				{
					AutoSize = AutoSizeButtons
				};

				foreach (string buttonValue in Buttons)
				{
					(string text, string value, bool isOther, bool isExclusive) = ButtonValueParser.ParseButtonValue(buttonValue, SplitValueTextPairs);

					if (isOther)
					{
						otherValues.Add(value);
					}

					if (isExclusive)
					{
						exclusiveValues.Add(value);
					}

					ButtonWithValue button = _grid.AddButton(text, value);

					if (Selectable)
					{
						button.State = ButtonStates.Selectable;
					}
				}

				_grid.Arrange();

				View input = _grid;

				if (Selectable && otherValues.Any())
				{
					otherEditor = new Editor
					{
						Keyboard = Keyboard.Default,
						HorizontalOptions = LayoutOptions.FillAndExpand,
						AutoSize = EditorAutoSizeOption.TextChanges
					};

					otherEditor.Unfocused += (s, e) =>
					{
						_otherResponseValue = otherEditor.Text;

						Complete = completeFunc(_grid.Buttons.Count(x => x.State == ButtonStates.Selected), true);
					};

					otherLabel = new Label { Text = $"{OtherResponseLabel ?? "Other Response"}:" };

					if (string.IsNullOrWhiteSpace(OtherResponseLabel) && otherValues.Count == 1 && GridButtons.FirstOrDefault(x => x.Value == otherValues.First()) is ButtonWithValue otherButton)
					{
						otherLabel.Text = otherButton.Text;
					}

					if (otherLabel.Text.EndsWith("::"))
					{
						otherLabel.Text = otherLabel.Text[0..^1];
					}

					otherLayout = new StackLayout
					{
						IsVisible = false,
						Children = { otherLabel, otherEditor }
					};

					input = new StackLayout
					{
						Children = { _grid, otherLayout }
					};
				}

				base.SetView(input);
			}

			return base.GetView(index);
		}
	}
}
