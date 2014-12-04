using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI.Properties
{
    public class DisplayYesNoUiProperty : UiProperty
    {
        public class ValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return ((bool)value) ? "Yes" : "No";
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public DisplayYesNoUiProperty(string labelText)
            : base(labelText, false)
        {
        }
    }
}
