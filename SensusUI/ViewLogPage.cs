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
        private Task _updateTask;
        private bool _update;

        public ViewLogPage()
        {
            ListView messageList = new ListView();

            _updateTask = Task.Run(() =>
                {
                    _update = true;
                    while (_update)
                    {
                        Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                            {
                                messageList.ItemsSource = null;
                                messageList.ItemsSource = UiBoundSensusServiceHelper.Get().Logger.Read(10);
                            });
                        Thread.Sleep(1000);
                    }
                });

            Button clearLogButton = new Button
            {
                Text = "Clear Log",
                Font = Font.SystemFontOfSize(20)
            };

            clearLogButton.Clicked += (o, e) => { UiBoundSensusServiceHelper.Get().Logger.Clear(); };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { messageList, clearLogButton }
            };
        }

        protected async override void OnDisappearing()
        {
            base.OnDisappearing();

            _update = false;
            await _updateTask;
        }
    }
}
