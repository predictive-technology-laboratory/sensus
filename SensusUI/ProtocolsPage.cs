using SensusService;
using System;
using System.IO;
using System.Linq;
using Xamarin.Forms;

namespace SensusUI
{
    public class ProtocolsPage : ContentPage
    {
        public static event EventHandler EditProtocol;

        private ListView _protocolsList;

        public ProtocolsPage()
        {
            Title = "Protocols";

            _protocolsList = new ListView();
            _protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            _protocolsList.ItemsSource = UiBoundSensusServiceHelper.Get().RegisteredProtocols;

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { _protocolsList }
            };

            ToolbarItems.Add(new ToolbarItem("Open", null, () =>
                {
                    if (_protocolsList.SelectedItem != null)
                        EditProtocol(_protocolsList.SelectedItem, null);
                }));

            ToolbarItems.Add(new ToolbarItem("+", null, () =>
                {
                    UiBoundSensusServiceHelper.Get().RegisterProtocol(new Protocol("New Protocol", true));

                    _protocolsList.ItemsSource = null;
                    _protocolsList.ItemsSource = UiBoundSensusServiceHelper.Get().RegisteredProtocols;
                }));

            ToolbarItems.Add(new ToolbarItem("-", null, async () =>
                {
                    if (_protocolsList.SelectedItem != null)
                    {
                        Protocol protocolToRemove = _protocolsList.SelectedItem as Protocol;

                        if (await DisplayAlert("Delete " + protocolToRemove.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                        {
                            await UiBoundSensusServiceHelper.Get().StopProtocolAsync(protocolToRemove, true);

                            try { Directory.Delete(protocolToRemove.StorageDirectory, true); }
                            catch (Exception ex) { UiBoundSensusServiceHelper.Get().Logger.Log("Failed to delete protocol storage directory \"" + protocolToRemove.StorageDirectory + "\":  " + ex.Message, LoggingLevel.Normal); }

                            _protocolsList.ItemsSource = _protocolsList.ItemsSource.Cast<Protocol>().Where(p => p != protocolToRemove);
                            _protocolsList.SelectedItem = null;
                        }
                    }
                }));
        }
    }
}
