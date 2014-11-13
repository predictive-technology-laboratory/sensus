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
        public DataStoresPage(Protocol protocol, bool local)
        {
            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            Type dataStoreType = local ? typeof(LocalDataStore) : typeof(RemoteDataStore);

            foreach (DataStore dataStore in Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(dataStoreType)).Select(t => Activator.CreateInstance(t)))
            {
                Button createDataStoreButton = new Button
                {
                    Text = "Create New " + dataStore.Name
                };

                createDataStoreButton.Clicked += async (o, e) =>
                    {
                        await Navigation.PushAsync(new DataStorePage(dataStore, protocol, local));
                    };

                (Content as StackLayout).Children.Add(createDataStoreButton);
            }

            MessagingCenter.Subscribe<DataStorePage, object>(this, "NewLocalDataStore", async (s, a) => { await Navigation.PopAsync(); });
        }
    }
}
