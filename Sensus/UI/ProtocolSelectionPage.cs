using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolSelectionPage : NavigationPage
    {
        public ProtocolSelectionPage(List<Protocol> protocols)
        {
            ListView protocolListView = new ListView();
            protocolListView.ItemsSource = protocols;
            protocolListView.ItemTemplate = new DataTemplate(typeof(TextCell));
            protocolListView.SetBinding(TextCell.TextProperty, "Description");

            ContentPage p = new ContentPage()
            {
                Content = new StackLayout()
                {
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children = { protocolListView }
                }
            };

            Navigation.PushModalAsync(p);
        }
    }
}
