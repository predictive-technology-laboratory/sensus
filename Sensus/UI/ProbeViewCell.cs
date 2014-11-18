using Sensus.Probes;
using System;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProbeViewCell : ViewCell
    {
        private class ProbeRunningValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return "Status:  " + (((bool)value) ? "On" : "Off");
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class SupportedValueConverter : IValueConverter
        {
            public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if ((bool)value)
                    return "";
                else
                    return "Unsupported";
            }

            public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new System.NotImplementedException();
            }
        }

        public ProbeViewCell()
        {
            Label nameLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };
            nameLabel.SetBinding(Label.TextProperty, "Name");

            Label enabledLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(15)
            };
            enabledLabel.SetBinding(Label.TextProperty, new Binding("Enabled", stringFormat: "Enabled:  {0}"));

            Label supportedLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(15)
            };
            supportedLabel.SetBinding(Label.TextProperty, new Binding("Supported", converter: new SupportedValueConverter()));

            Label statusLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(15)
            };
            statusLabel.SetBinding(Label.TextProperty, new Binding("Running", converter: new ProbeRunningValueConverter()));

            StackLayout probeProperties = new StackLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { enabledLabel, supportedLabel, statusLabel }
            };

            View = new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Vertical,
                Spacing = 0,
                Padding = new Thickness(0),
                Children = { nameLabel, probeProperties }
            };
        }
    }
}
