using SensusService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xamarin.Forms;

namespace SensusUI
{
    public class ShareProtocolPage : ContentPage
    {
        public static event EventHandler<ShareProtocolEventArgs> ShareTapped;

        public ShareProtocolPage(Protocol protocol)
        {
            Title = "Share Protocol";

            List<View> views = new List<View>();

            Label label = new Label
            {
                Text = "How would you like to share?",
                Font = Font.SystemFontOfSize(20)
            };

            Picker picker = new Picker();

            foreach (Protocol.ShareMethod method in Enum.GetValues(typeof(Protocol.ShareMethod)))
                picker.Items.Add(method.ToString());

            picker.SelectedIndex = 0;

            StackLayout pickerStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { label, picker }
            };

            Button okayButton = new Button
            {
                Text = "Share",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            okayButton.Clicked += (o, e) =>
                {
                    if (picker.SelectedIndex >= 0)
                        ShareTapped(o, new ShareProtocolEventArgs(protocol, (Protocol.ShareMethod)Enum.Parse(typeof(Protocol.ShareMethod), picker.Items[picker.SelectedIndex])));
                };

            StackLayout cancelOkStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { okayButton }
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { pickerStack, cancelOkStack }
            };
        }
    }
}
