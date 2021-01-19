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
		private string _value;

		private ButtonWithValue _correctButton;

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

		[EntryIntegerUiProperty("Number of Choices:", true, 3, true)]
		public int ChoiceCount { get; set; } = 4;

		[HiddenUiProperty]
		public override object CorrectValue { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				Random random = new Random();
				string word = Words[random.Next(Words.Count)].ToLower();

				int missingLetterIndex = random.Next(word.Length);
				string missingLetter = "";

				ButtonGridView wordGrid = new ButtonGridView(0, (o, s) => { })
				{
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				for (int letterIndex = 0; letterIndex < word.Length; letterIndex++)
				{
					string letter = word[letterIndex].ToString();

					if (letterIndex == missingLetterIndex)
					{
						_correctButton = wordGrid.AddButton("", "", Color.Default);

						missingLetter = letter;
					}
					else
					{
						wordGrid.AddButton(letter.ToUpper(), letter);
					}
				}

				wordGrid.Arrange();

				Label label = new Label()
				{
					Text = "Select a Tile:",
					HorizontalTextAlignment = TextAlignment.Center
				};

				ButtonGridView choiceGrid = new ButtonGridView(0, (o, s) =>
				{
					if (o is ButtonWithValue button)
					{
						_value = button.Value;

						if (button.Value == missingLetter)
						{
							_correctButton.Text = button.Text;

							button.IsVisible = false;
						}
					}

					Complete = true;
				})
				{
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				HashSet<string> choices = new HashSet<string>();

				CorrectValue = missingLetter;

				choices.Add(missingLetter);

				while (choices.Count < ChoiceCount)
				{
					string choice = ((char)('a' + random.Next(0, 26))).ToString();

					if (choices.Contains(choice) == false)
					{
						choices.Add(choice);
					}
				}

				foreach(string choice in choices.OrderBy(x => random.Next()))
				{
					choiceGrid.AddButton(choice.ToUpper(), choice);
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
	}
}
