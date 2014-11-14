using System;
using Xamarin.Forms;

namespace Sensus.UI.Properties
{
    public class EntryIntegerUiProperty : UiProperty
    {
        public class ValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value.ToString();
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                try
                {
                    return System.Convert.ToInt32(value);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public EntryIntegerUiProperty(string labelText, bool editable)
            : base(labelText, editable)
        {
        }
    }
}
