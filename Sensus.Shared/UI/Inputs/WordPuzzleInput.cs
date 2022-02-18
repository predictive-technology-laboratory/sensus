﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class WordPuzzleInput : Input, IVariableDefiningInput
	{
		private string _definedVariable;
		private List<string> _value;
		private HashSet<int> _missingLetterIndexes;
		private ButtonGridView _choiceGrid;

		public override object Value => _value;

		public override bool Enabled
		{
			get
			{
				return _choiceGrid?.IsEnabled ?? false;
			}
			set
			{
				if (_choiceGrid != null)
				{
					_choiceGrid.IsEnabled = value;
				}
			}
		}

		public override string DefaultName => "Word Puzzle";

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

		[EditableListUiProperty("Words:", true, 2, true)]
		public List<string> Words { get; set; }

		[EntryIntegerUiProperty("Number of Missing Letters:", true, 3, true)]
		public int MissingLetterCount { get; set; } = 1;

		[EntryIntegerUiProperty("Number of Choices:", true, 3, true)]
		public int ChoiceCount { get; set; } = 4;

		[EntryIntegerUiProperty("Column Count:", true, 4, true)]
		public int ColumnCount { get; set; } = 8;

		[OnOffUiProperty("Leave Incorrect Value:", true, 5)]
		public bool LeaveIncorrectValue { get; set; }

		[HiddenUiProperty]
		public override object CorrectValue { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null && Words.Count > 0)
			{
				_value = new List<string>();
				_missingLetterIndexes = new HashSet<int>();

				Random random = new Random();
				string word = Words[random.Next(Words.Count)].ToLower();

				List<(int Index, string Letter)> choices = new List<(int, string)>();

				int missingLetterCount = Math.Min(MissingLetterCount, ChoiceCount - 1);

				while (choices.Count < ChoiceCount)
				{
					if (choices.Count < missingLetterCount)
					{
						int missingLetterIndex = random.Next(word.Length);
						string missingLetter = word[missingLetterIndex].ToString().ToUpper();

						if (choices.Any(x => x.Index == missingLetterIndex) == false && choices.Any(x => x.Letter == missingLetter) == false)
						{
							_missingLetterIndexes.Add(missingLetterIndex);

							choices.Add((missingLetterIndex, missingLetter));
						}
					}
					else
					{
						string missingLetter = ((char)('a' + random.Next(0, 26))).ToString().ToUpper();

						if (choices.Any(x => x.Letter == missingLetter) == false)
						{
							choices.Add((-1, missingLetter));
						}
					}
				}

				choices = choices.OrderBy(x => random.Next()).ToList();

				ButtonGridView wordGrid = new ButtonGridView(ColumnCount, null)
				{
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				for (int letterIndex = 0; letterIndex < word.Length; letterIndex++)
				{
					string letter = word[letterIndex].ToString();

					if (choices.Any(x => x.Index == letterIndex))
					{
						ButtonWithValue wordButton = wordGrid.AddButton("", "");

						wordButton.StyleClass = new[] { "MissingLetterButton" };
					}
					else
					{
						wordGrid.AddButton(letter.ToUpper(), letter);
					}
				}

				wordGrid.Arrange();

				ButtonWithValue[] wordButtons = wordGrid.Buttons.ToArray();

				_choiceGrid = new ButtonGridView(0, null)
				{
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				if (LeaveIncorrectValue == false)
				{
					DelayEnded += (s, e) =>
					{
						foreach (ButtonWithValue otherButton in _choiceGrid.Buttons)
						{
							if (otherButton.State == ButtonStates.Incorrect)
							{
								otherButton.State = ButtonStates.Default;
							}
						}
					};
				}

				foreach ((int letterIndex, string choice) in choices)
				{
					ButtonWithValue button = _choiceGrid.AddButton(choice.ToUpper(), choice);

					if (letterIndex < 0)
					{
						button.Clicked += (s, e) =>
						{
							if (Correct == false)
							{
								_value = _value.Union(new[] { button.Value }).OrderBy(x => x).ToList();

								foreach (ButtonWithValue otherButton in _choiceGrid.Buttons)
								{
									otherButton.State = ButtonStates.Default;
								}

								button.State = ButtonStates.Incorrect;

								if (_value.Count >= MissingLetterCount)
								{
									Complete = true;
								}
								else
								{
									Attempts += 1;
								}

								SetFeedback(false);
							}
						};
					}
					else
					{
						button.Clicked += (s, e) =>
						{
							_value = _value.Union(new[] { button.Value }).OrderBy(x => x).ToList();
							_missingLetterIndexes.Remove(letterIndex);

							foreach (ButtonWithValue otherButton in _choiceGrid.Buttons)
							{
								otherButton.State = ButtonStates.Default;
							}

							button.State = ButtonStates.Correct;

							wordButtons[letterIndex].State = ButtonStates.Correct;

							wordButtons[letterIndex].Text = choice.ToUpper();

							if (_value.Count >= MissingLetterCount)
							{
								Complete = true;
							}
							else
							{
								Attempts += 1;
							}

							SetFeedback(true);
						};
					}
				}

				Label label = new Label()
				{
					Text = "Select a Tile:",
					HorizontalTextAlignment = TextAlignment.Center
				};

				if (MissingLetterCount > 1)
				{
					label.Text = "Select Tiles:";
				}

				_choiceGrid.Arrange();

				StackLayout puzzleLayout = new StackLayout()
				{
					Children = { wordGrid, label, _choiceGrid }
				};

				base.SetView(puzzleLayout);
			}

			return base.GetView(index);
		}

		public override void Reset()
		{
			_value = new List<string>();

			base.Reset();
		}

		protected override bool IsCorrect(object deserializedValue)
		{
			return _missingLetterIndexes.Count == 0;
		}
	}
}
