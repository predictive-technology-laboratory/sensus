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
}
