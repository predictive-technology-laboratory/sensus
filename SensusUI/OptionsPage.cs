using SensusService;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace SensusUI
{
    public class OptionsPage : ContentPage
    {
        public static event EventHandler ViewLogTapped;

        public OptionsPage(SensusServiceHelper service)
        {
            Title = "Options";

            List<StackLayout> stacks = UiProperty.GetPropertyStacks(service);

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (StackLayout stack in stacks)
                contentLayout.Children.Add(stack);

            Button viewLogButton = new Button
            {
                Text = "View Sensus Log",
                Font = Font.SystemFontOfSize(20)
            };

            viewLogButton.Clicked += (o, e) =>
                {
                    ViewLogTapped(o, e);
                };

            contentLayout.Children.Add(viewLogButton);

            Button stopSensusButton = new Button
            {
                Text = "Stop Sensus",
                Font = Font.SystemFontOfSize(20)
            };

            stopSensusButton.Clicked += (o, e) =>
                {
                    UiBoundSensusServiceHelper.Get().Stop();
                };

            contentLayout.Children.Add(stopSensusButton);

            Content = contentLayout;
        }
    }
}
