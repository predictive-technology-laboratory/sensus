using Sensus.UI.UiProperties;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ButtonGridInput : Input, IVariableDefiningInput
	{
		private string _definedVariable;
		private string _value;

		public override object Value => _value;

		public override bool Enabled { get; set; }

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
		public int ColumnCount { get; set; } = 1;

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				ButtonGridView grid = new ButtonGridView(ColumnCount, (o, s) =>
				{
					if (o is ButtonWithValue button)
					{
						_value = button.Value;
					}

					Complete = true;
				});

				foreach (string button in Buttons)
				{
					grid.AddButton(button, button);
				}

				grid.Arrange();

				base.SetView(grid);
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
