using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class MainPage : NavigationPage
    {
        public MainPage()
        {
            List<Protocol> protocols = new List<Protocol>();
            protocols.Add(new Protocol("Test Protocol 1", true));
            protocols.Add(new Protocol("Test Protocol 2", true));

            ListView mainListView = new ListView();
            mainListView.ItemsSource = new object[] 
            {
                protocols[0]
            };

            mainListView.ItemTemplate = new DataTemplate(typeof(TextCell));
            mainListView.SetBinding(TextCell.TextProperty, "Description");
            mainListView.ItemSelected += async (o, e) =>
                {
                    Page drillDown = null;
                    if (e.SelectedItem is Protocol)
                        drillDown = new ProtocolSelectionPage(protocols);

                    if (drillDown != null)
                        await Navigation.PushAsync(drillDown);
                };

            ContentPage p = new ContentPage()
            {
                Content = new StackLayout()
                {
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children = { mainListView }
                }
            };

            Navigation.PushModalAsync(p);
        }
    }
}
