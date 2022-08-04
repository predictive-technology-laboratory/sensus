using CoreGraphics;
using Foundation;
using Sensus.iOS.UI;
using Sensus.UI.Inputs;
using System;
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

			bool resized = false;

			if (Control != null)
			{
				Element.SizeChanged += (s, e) =>
				{
					if (resized == false)
					{
						CGRect unconstrainedBounds = ((NSString)Control.TitleLabel.Text).GetBoundingRect(new CGSize(double.MaxValue, double.MaxValue), NSStringDrawingOptions.UsesLineFragmentOrigin, new UIStringAttributes { Font = Control.TitleLabel.Font }, null);
						CGRect constrainedBounds = ((NSString)Control.TitleLabel.Text).GetBoundingRect(new CGSize(Element.Width - Element.Padding.HorizontalThickness, double.MaxValue), NSStringDrawingOptions.UsesLineFragmentOrigin, new UIStringAttributes { Font = Control.TitleLabel.Font }, null);

						double difference = Element.Height - unconstrainedBounds.Height;

						Element.HeightRequest = Math.Max(Element.Height, constrainedBounds.Height + difference);

						resized = true;
					}
				};

				Control.TitleLabel.LineBreakMode = UILineBreakMode.WordWrap;
				Control.TitleLabel.Lines = 0;
				Control.TitleLabel.TextAlignment = UITextAlignment.Center;
			}
		}
	}
}
