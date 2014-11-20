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

            // for protocols registered with the service, we need to remove previous bindings to propertychanged events
            foreach (Protocol protocol in App.Get().SensusService.RegisteredProtocols)
            {
                protocol.ClearPropertyChangedDelegates();
                protocols.Add(protocol);
            }

            for (int i = 0; i < 5; ++i)
                protocols.Add(new Protocol("Test Protocol " + (i + 1), true));

            ListView protocolsList = new ListView();
            protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
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
