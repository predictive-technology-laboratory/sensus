using System;
using Xamarin.Forms;

namespace SensusUI
{
    /// <summary>
    /// Main Sensus page. First thing the user sees.
    /// </summary>
    public class MainPage : ContentPage
    {
        public static event EventHandler<ItemTappedEventArgs> ProtocolsTapped;
        public static event EventHandler<EventArgs> OptionsTapped;

        public MainPage()
        {
            Title = "Sensus";

            Label protocolsLabel = new Label
            {
                Text = "Protocols",
                Font = Font.SystemFontOfSize(20)
            };

            ListView mainList = new ListView();
            mainList.ItemTemplate = new DataTemplate(typeof(TextCell));
            mainList.ItemTemplate.SetBinding(TextCell.TextProperty, "Text");
            mainList.ItemsSource = new Label[] { protocolsLabel };
            mainList.ItemTapped += (o, e) =>
                {
                    mainList.SelectedItem = null;
                    if (e.Item == protocolsLabel)
                        ProtocolsTapped(o, e);
                };

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { mainList }
            };

            ToolbarItems.Add(new ToolbarItem("Options", null, () => OptionsTapped(null, null)));
        }
    }
}
