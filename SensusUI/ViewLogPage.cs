using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SensusUI
{
    public class ViewLogPage : ContentPage
    {
        public static event EventHandler RefreshTapped;

        public ViewLogPage()
        {
            ListView messageList = new ListView
            {
                ItemsSource = UiBoundSensusServiceHelper.Get().Logger.Read(int.MaxValue)
            };

            Button refreshButton = new Button
            {
                Text = "Refresh",
                Font = Font.SystemFontOfSize(20)
            };

            refreshButton.Clicked += (o,e) =>
                {
                    RefreshTapped(o, e);
                };

            Button clearLogButton = new Button
            {
                Text = "Clear Log",
                Font = Font.SystemFontOfSize(20)
            };

            clearLogButton.Clicked += async (o, e) =>
            {
                if (await DisplayAlert("Confirm Clear", "Do you wish to clear the log? This cannot be undone.", "OK", "Cancel"))
                {
                    UiBoundSensusServiceHelper.Get().Logger.Clear();
                    RefreshTapped(o, e);
                }
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { messageList, refreshButton, clearLogButton }
            };
        }
    }
}
