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
