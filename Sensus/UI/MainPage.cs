using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class MainPage : NavigationPage
    {
        public static MainPage Get()
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

            ContentPage rootPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children = { mainList}
                }
            };

            MainPage mainPage = new MainPage(rootPage);

            mainList.ItemTapped += async (o, e) =>
                {
                    Page drillDownPage = null;
                    if (e.Item == protocolsLabel)
                        drillDownPage = ProtocolSelectionPage.Get();

                    if (drillDownPage != null)
                        await mainPage.Navigation.PushAsync(drillDownPage);
                };

            return mainPage;
        }

        private MainPage(Page rootPage)
            : base(rootPage)
        {
            Title = "Sensus";
        }
    }
}
