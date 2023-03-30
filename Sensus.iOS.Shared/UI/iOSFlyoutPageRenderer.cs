using Sensus.iOS.UI;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(FlyoutPage), typeof(iOSFlyoutPageRenderer))]

namespace Sensus.iOS.UI
{
	public class iOSFlyoutPageRenderer : PhoneFlyoutPageRenderer
	{
		private bool _hasAppeared;

		public iOSFlyoutPageRenderer()
		{
			_hasAppeared = false;
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);

			if (_hasAppeared == false)
			{
				IVisualElementRenderer flyoutRenderer = Platform.GetRenderer(((FlyoutPage)Element).Flyout);

				UIView superView = flyoutRenderer.NativeView.Superview;

				flyoutRenderer.NativeView.RemoveFromSuperview();

				superView.AddSubview(flyoutRenderer.NativeView);

				_hasAppeared = false;
			}
		}
	}
}
