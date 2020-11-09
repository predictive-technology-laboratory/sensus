using Sensus.UI.UiProperties;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ReadOnlyTextInput : Input
	{
		public ReadOnlyTextInput()
		{
			StoreCompletionRecords = false;
			Complete = true;
			Required = false;
		}

		public override object Value
		{
			get
			{
				return Text;
			}
		}

		public override bool Enabled { get; set; }

		public override string DefaultName => "Text";

		[EditorUiProperty("Text", true, 2, true)]
		public string Text { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				Label label = CreateLabel(-1);

				label.Text = Text;

				base.SetView(label);
			}

			return base.GetView(index);
		}
	}
}
