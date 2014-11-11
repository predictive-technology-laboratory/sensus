using Sensus.Probes;
using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Main Sensus page. First thing the user sees.
    /// </summary>
    public class MainPage : NavigationPage
    {
        public MainPage()
            : base()
        {
            Label protocolsLabel = new Label
            {
                Text = "Protocols",
                Font = Font.SystemFontOfSize(20)
            };

            ListView mainList = new ListView();
            mainList.ItemTemplate = new DataTemplate(typeof(TextCell));
            mainList.ItemTemplate.SetBinding(TextCell.TextProperty, "Text");
            mainList.ItemsSource = new Label[] { protocolsLabel };
            mainList.ItemTapped += async (o, e) =>
                {
                    Page drillDownPage = null;
                    if (e.Item == protocolsLabel)
                        drillDownPage = new ProtocolsPage();

                    if (drillDownPage != null)
                        await Navigation.PushAsync(drillDownPage);
                };

            ContentPage rootPage = new ContentPage
            {
                Title = "Sensus",
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children = { mainList }
                }
            };

            Navigation.PushAsync(rootPage);
        }
    }
}
