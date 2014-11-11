using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolSelectionPage : ContentPage
    {
        public ProtocolSelectionPage()
        {
            Title = "Protocols";

            List<Protocol> protocols = new List<Protocol>();
            for (int i = 0; i < 20; ++i)
                protocols.Add(new Protocol("Test Protocol " + (i + 1), true));

            ListView protocolsList = new ListView();
            protocolsList.ItemTemplate = new DataTemplate(typeof(ProtocolViewCell));
            protocolsList.ItemsSource = protocols;
            protocolsList.ItemTapped += async (o, e) =>
                {
                    await Navigation.PushAsync(new ProtocolPage(e.Item as Protocol));
                };

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { protocolsList }
            };
        }
    }
}
