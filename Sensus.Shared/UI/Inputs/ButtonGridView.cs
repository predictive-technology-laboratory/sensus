using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ButtonGridView : Grid
	{
		protected readonly List<ButtonWithValue> _buttons;

		public ButtonGridView(int columnCount, EventHandler defaultClickEvent) : base()
		{
			_buttons = new List<ButtonWithValue>();
			
			ColumnCount = columnCount;

			if (defaultClickEvent != null)
			{
				DefaultClickEvent = defaultClickEvent;
			}
		}

		public ButtonGridView() : this(1, null)
		{

		}

		public int ColumnCount { get; private set; }

		public EventHandler DefaultClickEvent { get; set; }

		public ButtonWithValue AddButton(string text, string value)
		{
			return AddButton(text, value, Color.Default, null, Color.Default, Color.Default);
		}
		public ButtonWithValue AddButton(string text, string value, Color color)
		{
			return AddButton(text, value, color, null, Color.Default, color);
		}
		public ButtonWithValue AddButton(string text, string value, Color color, Color textColor)
		{
			return AddButton(text, value, color, null, textColor, color);
		}
		public ButtonWithValue AddButton(string text, string value, Color color, Color textColor, Color borderColor)
		{
			return AddButton(text, value, color, null, textColor, borderColor);
		}
		public ButtonWithValue AddButton(string text, string value, Color color, EventHandler clicked)
		{
			return AddButton(text, value, color, clicked, Color.Default, color);
		}
		public ButtonWithValue AddButton(string text, string value, Color color, EventHandler clicked, Color textColor)
		{
			return AddButton(text, value, color, clicked, textColor, Color.Default);
		}
		public ButtonWithValue AddButton(string text, string value, Color color, EventHandler clicked, Color textColor, Color borderColor)
		{
			ButtonWithValue button = new ButtonWithValue
			{
				Text = text,
				BackgroundColor = color,
				TextColor = textColor,
				BorderColor = borderColor,
				Value = value
			};

			if (clicked != null)
			{
				button.Clicked += clicked;
			}

			button.Clicked += DefaultClickEvent;

			_buttons.Add(button);

			return button;
		}

		public void Arrange()
		{
			ColumnDefinitions.Clear();
			RowDefinitions.Clear();
			Children.Clear();

			if (ColumnCount <= 0)
			{
				ColumnCount = _buttons.Count;
			}

			for (int column = 0; column < ColumnCount; column++)
			{
				ColumnDefinitions.Add(new ColumnDefinition());
			}

			int rowCount = (int)Math.Ceiling((double)_buttons.Count / ColumnCount);

			for (int row = 0; row < rowCount; row++)
			{
				RowDefinitions.Add(new RowDefinition());
			}

			for(int button = 0; button < _buttons.Count; button++)
			{
				Children.Add(_buttons[button], button % ColumnCount, button / ColumnCount);
			}
		}
	}
}
