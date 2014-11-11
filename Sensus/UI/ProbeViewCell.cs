using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProbeViewCell : ViewCell
    {
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

            Label stateLabel = new Label
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(15)
            };
            stateLabel.SetBinding(Label.TextProperty, new Binding("State", stringFormat: "State:  {0}"));

            StackLayout probeProperties = new StackLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { enabledLabel, stateLabel }
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
