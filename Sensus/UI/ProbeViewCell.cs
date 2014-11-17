using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProbeViewCell : ViewCell
    {
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

            Label runningLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(15)
            };
            runningLabel.SetBinding(Label.TextProperty, new Binding("Controller.Running", stringFormat: "Running:  {0}"));

            StackLayout probeProperties = new StackLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { enabledLabel, runningLabel }
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
