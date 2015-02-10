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

using SensusService;
using SensusService.DataStores;
using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;

namespace SensusUI
{
    public class DataStorePage : ContentPage
    {        
        public DataStorePage(Protocol protocol, DataStore dataStore, bool local)
        {
            Title = (local ? "Local" : "Remote") + " Data Store";

            List<StackLayout> stacks = UiProperty.GetPropertyStacks(dataStore);

            StackLayout buttonStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            stacks.Add(buttonStack);

            Button clearButton = new Button
            {
                Text = "Clear",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                IsEnabled = dataStore.Clearable
            };

            clearButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Clear data from " + dataStore.Name + "?", "This action cannot be undone.", "Clear", "Cancel"))
                        dataStore.Clear();
                };

            buttonStack.Children.Add(clearButton);

            if (local)
            {
                Button shareLocalDataButton = new Button
                {
                    Text = "Share",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20
                };

                shareLocalDataButton.Clicked += (o, e) =>
                    {
                        try
                        {
                            string sharePath = UiBoundSensusServiceHelper.Get(true).GetSharePath(".json");
                            StreamWriter shareFile = new StreamWriter(sharePath);
                            LocalDataStore localDataStore = dataStore as LocalDataStore;
                            foreach (Datum datum in localDataStore.GetDataForRemoteDataStore())
                                shareFile.WriteLine(datum.JSON);

                            shareFile.Close();

                            SensusServiceHelper.Get().ShareFileAsync(sharePath, "Sensus Data");
                        }
                        catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to share local data store:  " + ex.Message, LoggingLevel.Normal); }
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

            foreach (StackLayout stack in stacks)
                contentLayout.Children.Add(stack);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
