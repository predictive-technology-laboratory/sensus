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

using System.Collections.Generic;
using Sensus.Shared.DataStores;
using Sensus.Shared.UI.UiProperties;
using Sensus.Shared.DataStores.Local;
using Sensus.Shared.DataStores.Remote;
using Xamarin.Forms;

namespace Sensus.Shared.UI
{
    /// <summary>
    /// Displays a data store.
    /// </summary>
    public class DataStorePage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStorePage"/> class.
        /// </summary>
        /// <param name="protocol">Protocol to which data store is to be bound.</param>
        /// <param name="dataStore">Data store to display.</param>
        /// <param name="local">If set to <c>true</c>, the data store will be treated as a local data store.</param>
        /// <param name="newDataStore">If set to <c>true</c>, the data store will be treated as a new data store.</param>
        public DataStorePage(Protocol protocol, DataStore dataStore, bool local, bool newDataStore)
        {
            Title = (local ? "Local" : "Remote") + " Data Store";

            List<View> views = new List<View>();

            views.Add(new ContentView
                {
                    Content = new Label
                    { 
                        Text = dataStore.DisplayName,
                        FontSize = 20, 
                        FontAttributes = FontAttributes.Italic,
                        TextColor = Color.Accent,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    Padding = new Thickness(0, 10, 0, 10)
                });

            // property stacks all come from the data store passed in (i.e., a copy of the original on the protocol, if there is one)
            views.AddRange(UiProperty.GetPropertyStacks(dataStore));

            StackLayout buttonStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            views.Add(buttonStack);

            // clearing only applies to local data stores that already exist on protocols and are clearable. new local data stores don't have this option.
            if (local && !newDataStore && protocol.LocalDataStore.Clearable)
            {
                Button clearButton = new Button
                {
                    Text = "Clear",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20
                };

                clearButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Clear data from " + protocol.LocalDataStore.DisplayName + "?", "This action cannot be undone.", "Clear", "Cancel"))
                        protocol.LocalDataStore.Clear();  // clear the protocol's local data store
                };

                buttonStack.Children.Add(clearButton);
            }

            // sharing only applies to local data stores that already exist on protocols. new local data stores don't have this option.
            if (local && !newDataStore)
            {
                Button shareLocalDataButton = new Button
                {
                    Text = "Share",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20
                };

                shareLocalDataButton.Clicked += async (o, e) =>
                {
                    await Navigation.PushAsync(new ShareLocalDataStorePage(protocol.LocalDataStore));
                };

                buttonStack.Children.Add(shareLocalDataButton);
            }

            Button okayButton = new Button
            {
                Text = "OK",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20
            };

            okayButton.Clicked += async (o, e) =>
            {
                if (local)
                    protocol.LocalDataStore = dataStore as LocalDataStore;
                else
                    protocol.RemoteDataStore = dataStore as RemoteDataStore;

                await Navigation.PopAsync();
            };

            buttonStack.Children.Add(okayButton);

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (View view in views)
                contentLayout.Children.Add(view);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
