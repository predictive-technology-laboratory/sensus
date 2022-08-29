using Sensus.iOS.UI;
using Sensus.UI;
using System;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(TimePicker), typeof(iOSTimePickerRenderer))]

namespace Sensus.iOS.UI
{
	public class iOSTimePickerRenderer : TimePickerRenderer
	{
		public iOSTimePickerRenderer()
		{

		}

		protected override void OnElementChanged(ElementChangedEventArgs<TimePicker> e)
		{
			base.OnElementChanged(e);

			if (Control != null)
			{
				if (Control.InputView is UIDatePicker picker)
				{
					picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;

					if (Element is ConstrainedTimePicker constrainedPicker)
					{
						picker.MinimumDate = DateTime.MinValue.Add(constrainedPicker.MinimumTime).ToNSDate();
						picker.MaximumDate = DateTime.MinValue.Add(constrainedPicker.MaximumTime).ToNSDate();
					}
				}
			}
		}
	}
}
