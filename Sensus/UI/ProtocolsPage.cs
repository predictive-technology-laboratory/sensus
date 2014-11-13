using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolsPage : ContentPage
    {
        public ProtocolsPage()
        {
            Title = "Protocols";

            List<Protocol> protocols = new List<Protocol>();
            for (int i = 0; i < 5; ++i)
                protocols.Add(new Protocol("Test Protocol " + (i + 1), true));

            ListView protocolsList = new ListView();
            protocolsList.ItemTemplate = new DataTemplate(typeof(ProtocolViewCell));
            protocolsList.ItemsSource = protocols;
            protocolsList.ItemTapped += async (o, e) =>
            {
                Protocol protocol = (o as ListView).SelectedItem as Protocol;
                protocolsList.SelectedItem = null;
                await Navigation.PushAsync(new ProtocolPage(protocol));
            };

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { protocolsList }
            };
        }
    }
}
