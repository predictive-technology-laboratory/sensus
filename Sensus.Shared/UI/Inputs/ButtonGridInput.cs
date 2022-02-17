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

using Newtonsoft.Json;
using Sensus.UI.UiProperties;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ButtonGridInput : Input, IVariableDefiningInput
	{
		private string _definedVariable;
		private string _value;
		protected ButtonGridView _grid;
		static private Style _correctStyle;
		static private Style _incorrectStyle;
		static private Style _selectableStyle;
		static private Style _selectedStyle;

		private Style _defaultStyle;


		static ButtonGridInput()
		{
			_correctStyle = (Style)Application.Current.Resources["CorrectAnswerButton"];
			_incorrectStyle = (Style)Application.Current.Resources["IncorrectAnswerButton"];
			_selectableStyle = (Style)Application.Current.Resources["SelectableButton"];
			_selectedStyle = (Style)Application.Current.Resources["SelectedButton"];
		}

		public ButtonGridInput() : base()
		{
			ColumnCount = 1;

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

					if (GridButtons != null)
					{
						foreach (ButtonWithValue button in GridButtons)
						{
							button.IsEnabled = value;
						}
					}
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

		[OnOffUiProperty("Leave Incorrect Value:", true, 4)]
		public bool LeaveIncorrectValue { get; set; }

		[JsonIgnore]
		public List<ButtonWithValue> GridButtons => _grid?.Buttons.ToList() ?? new List<ButtonWithValue>();

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				if (Selectable)
				{
					_defaultStyle = _selectableStyle;
				}
				
				if (LeaveIncorrectValue == false)
				{
					DelayEnded += (s, e) =>
					{
						foreach (ButtonWithValue gridButton in _grid.Buttons)
						{
							if (gridButton.Style != null)
							{
								if (gridButton.Style == _incorrectStyle)
								{
									gridButton.Style = _defaultStyle;
								}
							}
						}
					};
				}

				_grid = new ButtonGridView(ColumnCount, (s, e) =>
				{
					ButtonWithValue button = (ButtonWithValue)s;

					_value = button.Value;

					Complete = true;

					foreach (ButtonWithValue gridButton in _grid.Buttons)
					{
						gridButton.Style = _defaultStyle;
					}

					if (CorrectValue != null)
					{
						if (Correct)
						{
							button.Style = _correctStyle; // (Style)Application.Current.Resources["CorrectAnswerButton"];
						}
						else
						{
							button.Style = _incorrectStyle; // (Style)Application.Current.Resources["IncorrectAnswerButton"];
						}
					}
					else if (Selectable)
					{
						button.Style = _selectedStyle; // (Style)Application.Current.Resources["SelectedButton"];
					}
				});

				foreach (string buttonValue in Buttons)
				{
					ButtonWithValue button = _grid.AddButton(buttonValue, buttonValue);

					button.Style = _defaultStyle;
				}

				_grid.Arrange();

				base.SetView(_grid);
			}

			return base.GetView(index);
		}
	}
}
