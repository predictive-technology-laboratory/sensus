using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class DarkModeCompatibleTextCell : TextCell
	{
		public DarkModeCompatibleTextCell()
		{
			Application.Current.Resources.TryGetValue("TextColor", out object textColor);

			TextColor = (Color)textColor;
		}
	}
}
