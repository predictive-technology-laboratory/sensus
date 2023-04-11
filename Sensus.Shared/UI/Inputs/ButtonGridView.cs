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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
			return AddButton(text, value, null);
		}
		public ButtonWithValue AddButton(string text, string value, EventHandler clicked)
		{
			ButtonWithValue button = new ButtonWithValue
			{
				Text = text,
				Value = value
			};

			if (AutoSize)
			{
				button.HeightRequest = -1;
			}

			if (DefaultClickEvent != null)
			{
				button.Clicked += DefaultClickEvent;
			}

			if (clicked != null)
			{
				button.Clicked += clicked;
			}

			_buttons.Add(button);

			return button;
		}
		public ButtonWithValue AddButton(string text, string value, Color color)
		{
			ButtonWithValue button = AddButton(text, value, null);

			button.BackgroundColor = color;

			return button;
		}
		public ButtonWithValue AddButton(string text, string value, Color color, Color textColor)
		{
			ButtonWithValue button = AddButton(text, value, null);

			button.BackgroundColor = color;
			button.TextColor = textColor;

			return button;
		}
		public ButtonWithValue AddButton(string text, string value, Color color, Color textColor, Color borderColor)
		{
			ButtonWithValue button = AddButton(text, value, null);

			button.BackgroundColor = color;
			button.TextColor = textColor;
			button.BorderColor = borderColor;

			return button;
		}
		public ButtonWithValue AddButton(string text, string value, Color color, EventHandler clicked)
		{
			ButtonWithValue button = AddButton(text, value, clicked);

			button.BackgroundColor = color;

			return button;
		}
		public ButtonWithValue AddButton(string text, string value, Color color, EventHandler clicked, Color textColor)
		{
			ButtonWithValue button = AddButton(text, value, clicked);

			button.BackgroundColor = color;
			button.TextColor = textColor;

			return button;
		}
		public ButtonWithValue AddButton(string text, string value, Color color, EventHandler clicked, Color textColor, Color borderColor)
		{
			ButtonWithValue button = AddButton(text, value, clicked);

			button.BackgroundColor = color;
			button.TextColor = textColor;
			button.BorderColor = borderColor;

			return button;
		}

		public IEnumerable<ButtonWithValue> Buttons => _buttons.ToArray();

		public bool AutoSize { get; set; }

		protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (propertyName == nameof(IsEnabled))
			{
				foreach (ButtonWithValue button in _buttons)
				{
					button.IsEnabled = IsEnabled;
				}
			}

			base.OnPropertyChanged(propertyName);
		}

		public void Arrange()
		{
			ColumnDefinitions.Clear();
			RowDefinitions.Clear();
			Children.Clear();

			int columnCount = ColumnCount;

			if (columnCount <= 0 || _buttons.Count < columnCount)
			{
				columnCount = _buttons.Count;
			}

			for (int column = 0; column < columnCount; column++)
			{
				ColumnDefinitions.Add(new ColumnDefinition());
			}

			int rowCount = (int)Math.Ceiling((double)_buttons.Count / columnCount);

			for (int row = 0; row < rowCount; row++)
			{
				RowDefinitions.Add(new RowDefinition());
			}

			for (int button = 0; button < _buttons.Count; button++)
			{
				Children.Add(_buttons[button], button % columnCount, button / columnCount);
			}
		}
	}
}
