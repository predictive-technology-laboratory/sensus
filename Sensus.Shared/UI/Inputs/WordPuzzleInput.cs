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

		public override object Value => _value;

		public override bool Enabled { get; set; }

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

		[HiddenUiProperty]
		public override object CorrectValue { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
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
						string missingLetter = word[missingLetterIndex].ToString();

						if (choices.Any(x => x.Index == missingLetterIndex) == false && choices.Any(x => x.Letter == missingLetter.ToUpper()) == false)
						{
							_missingLetterIndexes.Add(missingLetterIndex);

							choices.Add((missingLetterIndex, missingLetter.ToUpper()));
						}
					}
					else
					{
						string missingLetter = ((char)('a' + random.Next(0, 26))).ToString();

						if (choices.Any(x => x.Letter == missingLetter) == false)
						{
							choices.Add((-1, missingLetter));
						}
					}
				}

				choices = choices.OrderBy(x => random.Next()).ToList();

				ButtonGridView wordGrid = new ButtonGridView(0, null)
				{
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				for (int letterIndex = 0; letterIndex < word.Length; letterIndex++)
				{
					string letter = word[letterIndex].ToString();

					if (choices.Any(x => x.Index == letterIndex))
					{
						ButtonWithValue wordButton = wordGrid.AddButton("", "");

						wordButton.Style = (Style)Application.Current.Resources["MissingLetterButton"];
					}
					else
					{
						wordGrid.AddButton(letter.ToUpper(), letter);
					}
				}

				wordGrid.Arrange();

				ButtonWithValue[] wordButtons = wordGrid.Buttons.ToArray();

				ButtonGridView choiceGrid = new ButtonGridView(0, null)
				{
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				foreach ((int letterIndex, string choice) in choices)
				{
					ButtonWithValue button = choiceGrid.AddButton(choice.ToUpper(), choice);

					if (letterIndex < 0)
					{
						button.Clicked += (s, e) =>
						{
							_value = _value.Union(new[] { button.Value }).OrderBy(x => x).ToList();

							button.Style = (Style)Application.Current.Resources["IncorrectAnswerButton"];

							if (_value.Count >= MissingLetterCount)
							{
								Complete = true;
							}
							else
							{
								Attempts += 1;
							}

							SetFeedback(false);
						};
					}
					else
					{
						button.Clicked += (s, e) =>
						{
							_value = _value.Union(new[] { button.Value }).OrderBy(x => x).ToList();
							_missingLetterIndexes.Remove(letterIndex);

							wordButtons[letterIndex].Style = (Style)Application.Current.Resources["CorrectAnswerButton"];

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

				choiceGrid.Arrange();

				StackLayout puzzleLayout = new StackLayout()
				{
					Children = { wordGrid, label, choiceGrid }
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
