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
        public static event EventHandler EditLocalDataStoreTapped;
        public static event EventHandler CreateLocalDataStoreTapped;
        public static event EventHandler EditRemoteDataStoreTapped;
        public static event EventHandler CreateRemoteDataStoreTapped;
        public static event EventHandler<ItemTappedEventArgs> ProbeTapped;

        private class DataStoreValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                DataStore dataStore = value as DataStore;
                return dataStore == null ? "None" : parameter + " Data Store:  " + dataStore.Name;
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
            probesList.ItemTemplate.SetBinding(TextCell.TextProperty, "DisplayName");
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
            Button editLocalDataStoreButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20),
                BindingContext = protocol
            };

            editLocalDataStoreButton.SetBinding(Button.TextProperty, new Binding("LocalDataStore", converter: new DataStoreValueConverter(), converterParameter: "Local"));
            editLocalDataStoreButton.Clicked += (o, e) =>
                {
                    EditLocalDataStoreTapped(protocol, e);
                };

            Button createLocalDataStoreButton = new Button
            {
                Text = "+",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            createLocalDataStoreButton.Clicked += (o, e) =>
                {
                    CreateLocalDataStoreTapped(protocol, e);
                };

            Button clearLocalDataStoreButton = new Button
            {
                Text = "Clear",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            clearLocalDataStoreButton.Clicked += (o, e) =>
                {
                    if (protocol.LocalDataStore != null)
                        protocol.LocalDataStore.Clear();
                };

            StackLayout localDataStoreStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { editLocalDataStoreButton, createLocalDataStoreButton, clearLocalDataStoreButton }
            };

            views.Add(localDataStoreStack);

            Button editRemoteDataStoreButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20),
                BindingContext = protocol
            };

            editRemoteDataStoreButton.SetBinding(Button.TextProperty, new Binding("RemoteDataStore", converter: new DataStoreValueConverter(), converterParameter: "Remote"));
            editRemoteDataStoreButton.Clicked += (o, e) =>
                {
                    EditRemoteDataStoreTapped(protocol, e);
                };

            Button createRemoteDataStoreButton = new Button
            {
                Text = "+",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            createRemoteDataStoreButton.Clicked += (o, e) =>
                {
                    CreateRemoteDataStoreTapped(protocol, e);
                };

            StackLayout remoteDataStoreStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { editRemoteDataStoreButton, createRemoteDataStoreButton }
            };

            views.Add(remoteDataStoreStack);
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
