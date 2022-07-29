using Sensus.iOS.UI;
using Sensus.UI.Inputs;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ButtonWithValue), typeof(iOSButtonWithValueRenderer))]

namespace Sensus.iOS.UI
{
	public class iOSButtonWithValueRenderer : ButtonRenderer
	{
		public iOSButtonWithValueRenderer()
		{

		}

		protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
		{
			base.OnElementChanged(e);

			if (Control != null)
			{
				Control.TitleLabel.LineBreakMode = UILineBreakMode.WordWrap;
				Control.TitleLabel.Lines = 0;
				Control.TitleLabel.TextAlignment = UITextAlignment.Center;
			}
		}
	}
}
