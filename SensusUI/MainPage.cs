using SensusService;
using SensusUI.UiProperties;
using System;
using Xamarin.Forms;

namespace SensusUI
{
    /// <summary>
    /// Main Sensus page. First thing the user sees.
    /// </summary>
    public class MainPage : ContentPage
    {
        public static event EventHandler ViewProtocolsTapped;
        public static event EventHandler ViewLogTapped;

        public MainPage(SensusServiceHelper service)
        {
            Title = "Sensus";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            Button viewProtocolsButton = new Button
            {
                Text = "View Protocols",
                Font = Font.SystemFontOfSize(20)
            };

            viewProtocolsButton.Clicked += (o, e) =>
                {
                    ViewProtocolsTapped(o, e);
                };

            contentLayout.Children.Add(viewProtocolsButton);

            Button viewLogButton = new Button
            {
                Text = "View Log",
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

            stopSensusButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Stop Sensus?", "Are you sure you want to stop Sensus?", "OK", "Cancel"))
                        await UiBoundSensusServiceHelper.Get().StopAsync();
                };

            contentLayout.Children.Add(stopSensusButton);

            foreach (StackLayout serviceStack in UiProperty.GetPropertyStacks(service))
                contentLayout.Children.Add(serviceStack);

            Content = contentLayout;
        }
    }
}
