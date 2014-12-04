using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Linq;
using System.IO;

namespace Sensus.UI
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
            _protocolsList.ItemsSource = App.Get().SensusService.RegisteredProtocols;

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { _protocolsList }
            };

            ToolbarItems.Add(new ToolbarItem("Open", null, new Action(() =>
                {
                    if (_protocolsList.SelectedItem != null)
                        EditProtocol(_protocolsList.SelectedItem, null);
                })));

            ToolbarItems.Add(new ToolbarItem("+", null, new Action(() =>
                {
                    App.Get().SensusService.RegisterProtocol(new Protocol(1, "New Protocol", true));

                    _protocolsList.ItemsSource = null;
                    _protocolsList.ItemsSource = App.Get().SensusService.RegisteredProtocols;
                })));

            ToolbarItems.Add(new ToolbarItem("-", null, new Action(async () =>
                {
                    if (_protocolsList.SelectedItem != null)
                    {
                        Protocol protocolToRemove = _protocolsList.SelectedItem as Protocol;

                        if (await DisplayAlert("Delete " + protocolToRemove.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                        {
                            await App.Get().SensusService.StopProtocolAsync(protocolToRemove, true);

                            try { Directory.Delete(protocolToRemove.StorageDirectory, true); }
                            catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to delete protocol storage directory \"" + protocolToRemove.StorageDirectory + "\":  " + ex.Message); }

                            _protocolsList.ItemsSource = _protocolsList.ItemsSource.Cast<Protocol>().Where(p => p != protocolToRemove);
                            _protocolsList.SelectedItem = null;
                        }
                    }
                })));
        }
    }
}
