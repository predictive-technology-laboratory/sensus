using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace SensusUI
{
    public class ViewLogPage : ContentPage
    {
        public ViewLogPage()
        {
            Editor editor = new Editor
            {
                IsEnabled = false,
            };

            UiBoundSensusServiceHelper.Get().Logger.MessageLogged += (o, m) =>
                {
                    lock (editor)
                        editor.Text += m;
                };

            Button clearLogButton = new Button
            {
                Text = "Clear Log",
                Font = Font.SystemFontOfSize(20)
            };

            clearLogButton.Clicked += (o, e) =>
                {
                    UiBoundSensusServiceHelper.Get().Logger.Clear();

                    lock (editor)
                        editor.Text = UiBoundSensusServiceHelper.Get().Logger.GetText();
                };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { clearLogButton, editor }
            };
        }
    }
}
