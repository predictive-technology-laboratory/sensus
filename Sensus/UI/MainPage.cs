using Sensus.Probes;
using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Main Sensus page. First thing the user sees.
    /// </summary>
    public class MainPage : NavigationPage
    {
        public const string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "sensus_log.txt");

        public MainPage()
            : base()
        {
            Console.SetError(new StandardOutWriter(LogPath, true, true));

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
