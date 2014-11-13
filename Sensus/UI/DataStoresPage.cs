using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using Xamarin.Forms;
using Sensus.DataStores.Local;
using Sensus.DataStores;
using Sensus.DataStores.Remote;

namespace Sensus.UI
{
    public class DataStoresPage : ContentPage
    {
        private class DataStoreValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (value == parameter) ? "Current:  " + (value as DataStore).Name : "Create New " + (value as DataStore).Name;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public DataStoresPage(Protocol protocol, bool local)
        {
            DataStore current = local ? protocol.LocalDataStore as DataStore : protocol.RemoteDataStore as DataStore;
            Type dataStoreType = local ? typeof(LocalDataStore) : typeof(RemoteDataStore);

            List<object> dataStores = new List<object>();

            if (current != null)
                dataStores.Add(current);

            dataStores.AddRange(Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(dataStoreType)).Select(t => Activator.CreateInstance(t)).ToList());

            ListView dataStoresList = new ListView();
            dataStoresList.ItemTemplate = new DataTemplate(typeof(TextCell));
            dataStoresList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", converterParameter: current, converter: new DataStoreValueConverter()));
            dataStoresList.ItemsSource = dataStores;
            dataStoresList.ItemTapped += async (o, e) =>
                {
                    await Navigation.PushAsync(new DataStorePage(e.Item as DataStore));
                };

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
                Children = { dataStoresList }
            };
        }
    }
}
