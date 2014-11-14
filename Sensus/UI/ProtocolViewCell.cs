using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolViewCell : ViewCell
    {
        private class ProbeCountValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                List<Probe> probes = value as List<Probe>;
                return "Probes:  " + probes.Count(p => p.Enabled) + " of " + probes.Count + " enabled";
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public ProtocolViewCell()
        {
            Label nameLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };
            nameLabel.SetBinding(Label.TextProperty, "Name");

            Label runningLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(15),
            };
            runningLabel.SetBinding(Label.TextProperty, new Binding("Running", stringFormat: "Running:  {0}"));

            Label probesLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(15)
            };
            probesLabel.SetBinding(Label.TextProperty, new Binding("Probes", converter: new ProbeCountValueConverter()));

            StackLayout protocolProperties = new StackLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { runningLabel, probesLabel }
            };

            View = new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Vertical,
                Spacing = 0,
                Padding = new Thickness(0),
                Children = { nameLabel, protocolProperties }
            };
        }
    }
}
