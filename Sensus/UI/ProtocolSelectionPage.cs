using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolSelectionPage : ContentPage
    {
        public static ContentPage Get()
        {
            List<Protocol> protocols = new List<Protocol>();
            for (int i = 0; i < 20; ++i)
                protocols.Add(new Protocol("Test Protocol " + (i + 1), true));

            ListView protocolsList = new ListView();
            protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            protocolsList.ItemsSource = protocols;

            ProtocolSelectionPage page = new ProtocolSelectionPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children = { protocolsList }
                }
            };

            protocolsList.ItemTapped += async (o, e) =>
                {
                    Protocol p = e.Item as Protocol;
                };

            return page;
        }

        private ProtocolSelectionPage()
        {
            Title = "Protocols";
        }
    }
}
