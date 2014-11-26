using Sensus.DataStores;
using Sensus.Probes;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolPage : ContentPage
    {
        public static event EventHandler CreateLocalDataStoreTapped;
        public static event EventHandler CreateRemoteDataStoreTapped;
        public static event EventHandler<ItemTappedEventArgs> ProbeTapped;

        private class DataStoreValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                DataStore dataStore = value as DataStore;

                return value == null ? "Create " + parameter + " Data Store" : parameter + " Data Store:  " + dataStore.Name;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class ProbeDetailValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                Datum mostRecent = value as Datum;
                return mostRecent == null ? "----------" : mostRecent.DisplayDetail + Environment.NewLine + mostRecent.Timestamp;
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

            List<View> views = new List<View>();

            views.AddRange(UiProperty.GetPropertyStacks(protocol));

            #region probes
            ListView probesList = new ListView();
            probesList.ItemTemplate = new DataTemplate(typeof(TextCell));
            probesList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            probesList.ItemTemplate.SetBinding(TextCell.DetailProperty, new Binding("MostRecentlyStoredDatum", converter: new ProbeDetailValueConverter()));
            probesList.ItemsSource = protocol.Probes;
            probesList.ItemTapped += (o, e) =>
                {
                    probesList.SelectedItem = null;
                    ProbeTapped(o, e);
                };

            views.Add(probesList);
            #endregion

            #region data stores
            Button localDataStoreButton = new Button
            {
                Text = "Create Local Data Store",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20),
                BindingContext = protocol
            };

            localDataStoreButton.SetBinding(Button.TextProperty, new Binding("LocalDataStore", converter: new DataStoreValueConverter(), converterParameter: "Local"));
            localDataStoreButton.Clicked += (o, e) =>
                {
                    CreateLocalDataStoreTapped(protocol, e);
                };

            views.Add(localDataStoreButton);

            Button remoteDataStoreButton = new Button
            {
                Text = "Create Remote Data Store",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20),
                BindingContext = protocol
            };

            remoteDataStoreButton.SetBinding(Button.TextProperty, new Binding("RemoteDataStore", converter: new DataStoreValueConverter(), converterParameter: "Remote"));
            remoteDataStoreButton.Clicked += (o, e) =>
                {
                    CreateRemoteDataStoreTapped(protocol, e);
                };

            views.Add(remoteDataStoreButton);
            #endregion

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            foreach (View view in views)
                (Content as StackLayout).Children.Add(view);
        }
    }
}
