using System;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class ConstrainedTimePicker : TimePicker
	{
		public static readonly BindableProperty MinimumTimeProperty = BindableProperty.Create("MinimumTime", typeof(TimeSpan), typeof(ConstrainedTimePicker), new TimeSpan(0, 0, 0));
		public static readonly BindableProperty MaximumTimeProperty = BindableProperty.Create("MaximumTime", typeof(TimeSpan), typeof(ConstrainedTimePicker), new TimeSpan(24, 0, 0));

		public ConstrainedTimePicker()
		{

		}

		public TimeSpan MinimumTime
		{
			get { return (TimeSpan)GetValue(MinimumTimeProperty); }
			set { SetValue(MinimumTimeProperty, value); }
		}

		public TimeSpan MaximumTime
		{
			get { return (TimeSpan)GetValue(MaximumTimeProperty); }
			set { SetValue(MaximumTimeProperty, value); }
		}
	}
}
