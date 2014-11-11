using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProbePage : ContentPage
    {
        public ProbePage(Probe p)
        {
            BindingContext = p;

            SetBinding(TitleProperty, new Binding("Name"));

            Label nameLabel = new Label
            {
                Text = "Name:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Entry nameEntry = new Entry();
            nameEntry.BindingContext = BindingContext;
            nameEntry.SetBinding(Entry.TextProperty, "Name");

            StackLayout nameStack = new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { nameLabel, nameEntry }
            };

            Label statusLabel = new Label
            {
                Text = "Status:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Switch statusSwitch = new Switch();
            statusSwitch.BindingContext = BindingContext;
            statusSwitch.SetBinding(Switch.IsToggledProperty, "Enabled");

            StackLayout statusStack = new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { statusLabel, statusSwitch }
            };

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
                Children = { nameStack, statusStack }
            };
        }
    }
}
