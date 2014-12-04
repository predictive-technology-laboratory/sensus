using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI.Properties
{
    public class DisplayStringUiProperty : UiProperty
    {
        public class ValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value.ToString();
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public DisplayStringUiProperty(string labelText)
            : base(labelText, false)
        {
        }
    }
}
