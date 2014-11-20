using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Linq;

namespace Sensus.UI
{
    public class ProtocolsPage : ContentPage
    {
        public static event EventHandler<ItemTappedEventArgs> ProtocolTapped;
        public static event EventHandler NewProtocolTapped;
        public static event EventHandler RemoveSelectedProtocolTapped;

        private ListView _protocolsList;

        public ProtocolsPage()
        {
            Title = "Protocols";

            _protocolsList = new ListView();
            _protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            _protocolsList.ItemsSource = App.Get().SensusService.RegisteredProtocols;
            _protocolsList.ItemTapped += (o, e) =>
            {
                _protocolsList.SelectedItem = null;
                ProtocolTapped(o, e);
            };

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { _protocolsList }
            };

            ToolbarItems.Add(new ToolbarItem("+", null, new Action(() => NewProtocolTapped(this, null))));
            ToolbarItems.Add(new ToolbarItem("-", null, new Action(() => RemoveSelectedProtocolTapped(this, null))));
        }

        public void AddProtocol(Protocol protocol)
        {
            List<Protocol> protocols = _protocolsList.ItemsSource.Cast<Protocol>().ToList();
            protocols.Add(protocol);

            _protocolsList.ItemsSource = protocols;
        }

        public void RemoveSelectedProtocol()
        {
            if (_protocolsList.SelectedItem != null)
            {
                List<Protocol> protocols = _protocolsList.ItemsSource.Cast<Protocol>().ToList();
                protocols.Remove(_protocolsList.SelectedItem as Protocol);

                _protocolsList.ItemsSource = protocols;
            }
        }
    }
}
