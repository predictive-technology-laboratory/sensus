#region copyright
// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using SensusService.DataStores;
using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using System;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace SensusUI
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
