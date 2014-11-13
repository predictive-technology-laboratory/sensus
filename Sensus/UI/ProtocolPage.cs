using Sensus.DataStores;
using Sensus.DataStores.Local;
using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolPage : ContentPage
    {
        private class DataStoreValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                DataStore dataStore = value as DataStore;

                return value == null ? "Create New " + parameter + " Data Store" : parameter + " Data Store:  " + dataStore.Name;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }


        public ProtocolPage(Protocol protocol)
        {
            BindingContext = protocol;

            SetBinding(TitleProperty, new Binding("Name"));

            #region name
            Label nameLabel = new Label
            {
                Text = "Name:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Entry nameEntry = new Entry();
            nameEntry.BindingContext = protocol;
            nameEntry.SetBinding(Entry.TextProperty, "Name");

            StackLayout nameStack = new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { nameLabel, nameEntry }
            };
            #endregion

            #region status
            Label statusLabel = new Label
            {
                Text = "Status:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Switch statusSwitch = new Switch();
            statusSwitch.BindingContext = protocol;
            statusSwitch.SetBinding(Switch.IsToggledProperty, "Running");

            StackLayout statusStack = new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { statusLabel, statusSwitch }
            };
            #endregion

            #region data stores
            Button localDataStoreButton = new Button
            {                
                Text = "Create New Local Data Store",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            localDataStoreButton.Clicked += async (o, e) =>
                {
                    await Navigation.PushAsync(new DataStoresPage(protocol, true));
                };
            #endregion

            #region probes
            ListView probesList = new ListView();
            probesList.ItemTemplate = new DataTemplate(typeof(ProbeViewCell));
            probesList.BindingContext = protocol;
            probesList.SetBinding(ListView.ItemsSourceProperty, "Probes");
            probesList.ItemTapped += async (o, e) =>
                {
                    Probe probe = e.Item as Probe;
                    probesList.SelectedItem = null;
                    await Navigation.PushAsync(new ProbePage(probe));
                };
            #endregion

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
                Children = { nameStack, statusStack, localDataStoreButton, probesList }
            };

            MessagingCenter.Subscribe<DataStorePage, object>(this, "NewLocalDataStore", (s, a) => { localDataStoreButton.Text = "Local Data Store:  " + protocol.LocalDataStore.Name; });
        }
    }
}
