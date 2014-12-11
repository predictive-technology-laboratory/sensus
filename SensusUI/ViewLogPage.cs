using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SensusUI
{
    public class ViewLogPage : ContentPage
    {
        public ViewLogPage()
        {
            Title = "Sensus Log";

            ListView messageList = new ListView();
            messageList.ItemTemplate = new DataTemplate(typeof(TextCell));
            messageList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", mode: BindingMode.OneWay));
            messageList.ItemsSource = new ObservableCollection<string>(UiBoundSensusServiceHelper.Get().Logger.Read(int.MaxValue));

            Button shareButton = new Button
            {
                Text = "Share",
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            shareButton.Clicked += (o, e) =>
                {
                    string path = null;
                    try
                    {
                        path = UiBoundSensusServiceHelper.Get().GetTempPath(".txt");
                        File.WriteAllLines(path, UiBoundSensusServiceHelper.Get().Logger.Read(int.MaxValue).ToArray());
                    }
                    catch (Exception ex)
                    {
                        UiBoundSensusServiceHelper.Get().Logger.Log("Failed to write log to temp file for sharing:  " + ex.Message, SensusService.LoggingLevel.Normal);
                        path = null;
                    }

                    if (path != null)
                        UiBoundSensusServiceHelper.Get().ShareFile(path, "Sensus Log:  " + Path.GetFileName(path));
                };

            Button clearButton = new Button
            {
                Text = "Clear",
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            clearButton.Clicked += async (o, e) =>
            {
                if (await DisplayAlert("Confirm", "Do you wish to clear the log? This cannot be undone.", "OK", "Cancel"))
                {
                    UiBoundSensusServiceHelper.Get().Logger.Clear();
                    messageList.ItemsSource = null;
                }
            };

            StackLayout shareClearStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { shareButton, clearButton }
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { messageList, shareClearStack }
            };
        }
    }
}
