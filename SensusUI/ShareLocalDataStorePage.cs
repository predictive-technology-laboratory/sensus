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

using System;
using Xamarin.Forms;
using System.Threading;
using SensusService.DataStores.Local;
using SensusService;
using System.IO;
using System.Collections.Generic;

namespace SensusUI
{
    public class ShareLocalDataStorePage : ContentPage
    {
        private CancellationTokenSource _cancellationTokenSource;

        public ShareLocalDataStorePage(LocalDataStore localDataStore)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Title = "Sharing Local Data Store";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };            

            Label statusLabel = new Label
            {
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            contentLayout.Children.Add(statusLabel);

            ProgressBar progressBar = new ProgressBar
            {
                Progress = 0,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            contentLayout.Children.Add(progressBar);

            Button cancelButton = new Button
            {
                Text = "Cancel",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
                        
            cancelButton.Clicked += async (o, e) =>
            {
                await Navigation.PopAsync();
            };

            contentLayout.Children.Add(cancelButton);
                                      
            new Thread(async () =>
                {                    
                    string sharePath = UiBoundSensusServiceHelper.Get(true).GetSharePath(".json");
                    bool errorWritingShareFile = false;
                    try
                    {              
                        Device.BeginInvokeOnMainThread(() => statusLabel.Text = "Gathering data...");
                        List<Datum> localData = localDataStore.GetDataForRemoteDataStore(_cancellationTokenSource.Token, progress =>
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                    {
                                        progressBar.ProgressTo(progress, 250, Easing.Linear);
                                    });
                            });                                

                        if (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                                {
                                    progressBar.ProgressTo(0, 0, Easing.Linear);
                                    statusLabel.Text = "Writing data to file...";
                                });
                            
                            using (StreamWriter shareFile = new StreamWriter(sharePath))
                            {
                                int dataWritten = 0;
                                foreach (Datum localDatum in localData)
                                {
                                    if (_cancellationTokenSource.IsCancellationRequested)
                                        break;
                                    
                                    shareFile.WriteLine(localDatum.GetJSON(localDataStore.Protocol.JsonAnonymizer));

                                    if ((++dataWritten % (localData.Count / 10)) == 0)
                                        Device.BeginInvokeOnMainThread(() => progressBar.ProgressTo(dataWritten / (double)localData.Count, 250, Easing.Linear));
                                }

                                shareFile.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorWritingShareFile = true;
                        string message = "Error writing share file:  " + ex.Message;
                        SensusServiceHelper.Get().FlashNotificationAsync(message);
                        SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                        await Navigation.PopAsync();
                    }

                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            File.Delete(sharePath);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if (!errorWritingShareFile)
                    {
                        Device.BeginInvokeOnMainThread(async () => await Navigation.PopAsync());
                        SensusServiceHelper.Get().ShareFileAsync(sharePath, "Sensus Data");
                    }

                }).Start();

            Content = new ScrollView
            { 
                Content = contentLayout
            };
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _cancellationTokenSource.Cancel();
        }
    }
}


