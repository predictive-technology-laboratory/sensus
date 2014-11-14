using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolsPage : ContentPage
    {
        public static event EventHandler<ItemTappedEventArgs> ProtocolTapped;

        public ProtocolsPage()
        {
            Title = "Protocols";

            List<Protocol> protocols = new List<Protocol>();
            for (int i = 0; i < 5; ++i)
                protocols.Add(new Protocol("Test Protocol " + (i + 1), true));

            ListView protocolsList = new ListView();
            protocolsList.ItemTemplate = new DataTemplate(typeof(ProtocolViewCell));
            protocolsList.ItemsSource = protocols;
            protocolsList.ItemTapped += (o, e) =>
            {
                protocolsList.SelectedItem = null;
                ProtocolTapped(o, e);
            };

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { protocolsList }
            };
        }
    }
}
