using SensusService.Exceptions;
using System;
using Xamarin.Forms;

namespace SensusUI.UiProperties
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
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        public DisplayYesNoUiProperty(string labelText, int order)
            : base(labelText, false, order)
        {
        }
    }
}
