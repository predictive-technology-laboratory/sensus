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
						wordGrid.AddButton("", "", Color.Default);

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

	//public class ButtonGridInput : Input, IVariableDefiningInput
	//{
	//	private string _definedVariable;
	//	private string _value;
	//	protected readonly List<ButtonWithValue> _buttons = new List<ButtonWithValue>();

	//	protected class ButtonWithValue : Button
	//	{
	//		public string Value { get; set; }
	//	}

	//	public override object Value => _value;

	//	public override bool Enabled { get; set; }

	//	public override string DefaultName => "Button Grid";

	//	[EntryStringUiProperty("Define Variable:", true, 2, false)]
	//	public string DefinedVariable
	//	{
	//		get
	//		{
	//			return _definedVariable;
	//		}
	//		set
	//		{
	//			_definedVariable = value?.Trim();
	//		}
	//	}

	//	[EditableListUiProperty("Buttons:", true, 2, true)]
	//	public List<string> Buttons { get; set; }

	//	[EntryIntegerUiProperty("Number of Columns:", true, 3, false)]
	//	public int ColumnCount { get; set; } = 1;

	//	public void Add(string text, string value)
	//	{
	//		Add(text, value, Color.Default, null, Color.Default, Color.Default);
	//	}
	//	public void Add(string text, string value, Color color)
	//	{
	//		Add(text, value, color, null, Color.Default, color);
	//	}
	//	public void Add(string text, string value, Color color, Color textColor)
	//	{
	//		Add(text, value, color, null, textColor, color);
	//	}
	//	public void Add(string text, string value, Color color, Color textColor, Color borderColor)
	//	{
	//		Add(text, value, color, null, textColor, borderColor);
	//	}
	//	public void Add(string text, string value, Color color, EventHandler clicked)
	//	{
	//		Add(text, value, color, clicked, Color.Default, color);
	//	}
	//	public void Add(string text, string value, Color color, EventHandler clicked, Color textColor)
	//	{
	//		Add(text, value, color, clicked, textColor, Color.Default);
	//	}
	//	public void Add(string text, string value, Color color, EventHandler clicked, Color textColor, Color borderColor)
	//	{
	//		void defaultClicked(object s, EventArgs e)
	//		{
	//			_value = value;

	//			Complete = true;
	//		}

	//		ButtonWithValue button = new ButtonWithValue
	//		{
	//			Text = text,
	//			BackgroundColor = color,
	//			TextColor = textColor,
	//			BorderColor = borderColor,
	//			Value = value
	//		};

	//		if (clicked != null)
	//		{
	//			button.Clicked += clicked;
	//		}

	//		button.Clicked += defaultClicked;

	//		_buttons.Add(button);
	//	}

	//	protected Grid Grid { get; private set; }

	//	public override View GetView(int index)
	//	{
	//		if (base.GetView(index) == null)
	//		{
	//			Grid grid = new Grid();

	//			_buttons.Clear();

	//			if (Buttons.Any())
	//			{
	//				foreach (string button in Buttons)
	//				{
	//					Add(button, button);
	//				}

	//				for (int column = 0; column < ColumnCount; column++)
	//				{
	//					grid.ColumnDefinitions.Add(new ColumnDefinition());
	//				}

	//				int rowCount = (int)Math.Ceiling((double)_buttons.Count / ColumnCount);

	//				for (int row = 0; row < rowCount; row++)
	//				{
	//					grid.RowDefinitions.Add(new RowDefinition());
	//				}

	//				for(int button = 0; button < _buttons.Count; button++)
	//				{
	//					grid.Children.Add(_buttons[button], button % ColumnCount, button / ColumnCount);
	//				}
	//			}

	//			Grid = grid;

	//			base.SetView(Grid);
	//		}

	//		return base.GetView(index);
	//	}
	//}
}
