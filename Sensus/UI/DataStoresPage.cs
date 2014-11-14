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
        public static event EventHandler<CreateDataStoreEventArgs> CreateDataStorePressed;

        public class CreateDataStoreEventArgs : EventArgs
        {
            public DataStore DataStore { get; set; }
            public Protocol Protocol { get; set; }
            public bool Local { get; set; }
        }

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

                createDataStoreButton.Clicked += (o, e) =>
                    {
                        CreateDataStorePressed(o, new CreateDataStoreEventArgs { DataStore = dataStore, Protocol = protocol, Local = local });
                    };

                (Content as StackLayout).Children.Add(createDataStoreButton);
            }
        }
    }
}
