using Sensus.UI.UiProperties;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class HyperlinkInput : Input
	{
		public override object Value
		{
			get
			{
				return null;
			}
		}

		public override bool Enabled { get; set; }

		public override string DefaultName => "Hyperlink";

		[EntryStringUiProperty("Url", true, 5, true)]
		public string Url { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				Label label = CreateLabel(-1);

				TapGestureRecognizer gesture = new TapGestureRecognizer()
				{
					NumberOfTapsRequired = 1
				};

				gesture.Tapped += (s, e) =>
				{
					Device.OpenUri(new System.Uri(Url));

					Complete = true;
				};

				label.GestureRecognizers.Add(gesture);

				base.SetView(label);
			}

			return base.GetView(index);
		}
	}
}
