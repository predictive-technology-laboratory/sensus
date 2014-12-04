using Sensus.DataStores;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using System;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class CreateDataStorePage : ContentPage
    {
        public static event EventHandler<ProtocolDataStoreEventArgs> CreateTapped;

        public CreateDataStorePage(ProtocolDataStoreEventArgs args)
        {
            Title = "Create " + (args.Local ? "Local" : "Remote") + " Data Store";

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            Type dataStoreType = args.Local ? typeof(LocalDataStore) : typeof(RemoteDataStore);

            foreach (DataStore dataStore in Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(dataStoreType)).Select(t => Activator.CreateInstance(t)))
            {
                Button createDataStoreButton = new Button
                {
                    Text = dataStore.Name
                };

                createDataStoreButton.Clicked += (o, e) =>
                    {
                        CreateTapped(o, new ProtocolDataStoreEventArgs { Protocol = args.Protocol, DataStore = dataStore, Local = args.Local });
                    };

                (Content as StackLayout).Children.Add(createDataStoreButton);
            }
        }
    }
}
