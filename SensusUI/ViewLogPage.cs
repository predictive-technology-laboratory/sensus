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
        private bool _paused;

        public ViewLogPage()
        {
            Title = "Message Log";

            _paused = false;

            ObservableCollection<string> messages = new ObservableCollection<string>(UiBoundSensusServiceHelper.Get().Logger.Read(int.MaxValue));

            ListView messageList = new ListView();
            messageList.ItemTemplate = new DataTemplate(typeof(TextCell));
            messageList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", mode: BindingMode.OneWay));
            messageList.ItemsSource = messages;

            UiBoundSensusServiceHelper.Get().Logger.MessageLogged += (o, e) =>
                {
                    lock (messages)
                        if (!_paused)
                            messages.Insert(0, e);
                };

            Button pauseButton = new Button
            {
                Text = "Pause",
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            pauseButton.Clicked += (o, e) =>
                {
                    lock (messages)
                    {
                        if (_paused)
                        {
                            messages.Clear();
                            foreach (string message in UiBoundSensusServiceHelper.Get().Logger.Read(int.MaxValue))
                                messages.Add(message);

                            pauseButton.Text = "Pause";
                        }
                        else
                            pauseButton.Text = "Resume";

                        _paused = !_paused;
                    }
                };

            Button clearButton = new Button
            {
                Text = "Clear Log",
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            clearButton.Clicked += async (o, e) =>
            {
                if (await DisplayAlert("Confirm", "Do you wish to clear the log? This cannot be undone.", "OK", "Cancel"))
                {
                    UiBoundSensusServiceHelper.Get().Logger.Clear();

                    lock (messages)
                        messages.Clear();
                }
            };

            StackLayout pauseClearStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { pauseButton, clearButton }
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { messageList, pauseClearStack }
            };
        }
    }
}
