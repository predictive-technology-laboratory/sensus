//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using Sensus.DataStores;
using Sensus.UI.UiProperties;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Xamarin.Forms;
using System;

namespace Sensus.UI
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
            if (local && !newDataStore && protocol.LocalDataStore is IClearableDataStore)
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
                    {
                        (protocol.LocalDataStore as IClearableDataStore).Clear();  // clear the protocol's local data store
                    }
                };

                buttonStack.Children.Add(clearButton);
            }

            // sharing only applies to local data stores
            if (local)
            {
                Button shareButton = new Button
                {
                    Text = "Share Data",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20,
                    IsEnabled = protocol.LocalDataStore?.HasDataToShare ?? false  // hide the share button if there is no data to share
                };

                shareButton.Clicked += async (o, e) =>
                {
                    await protocol.LocalDataStore?.ShareLocalDataAsync();
                };

                buttonStack.Children.Add(shareButton);
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
                {
                    protocol.LocalDataStore = dataStore as LocalDataStore;
                }
                else
                {
                    protocol.RemoteDataStore = dataStore as RemoteDataStore;
                }

                await Navigation.PopAsync();
            };

            buttonStack.Children.Add(okayButton);

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (View view in views)
            {
                contentLayout.Children.Add(view);
            }

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
