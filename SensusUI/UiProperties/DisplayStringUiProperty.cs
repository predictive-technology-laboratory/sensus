using System;
using Xamarin.Forms;

namespace SensusUI.UiProperties
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

        public DisplayStringUiProperty(string labelText, int order)
            : base(labelText, false, order)
        {
        }
    }
}
