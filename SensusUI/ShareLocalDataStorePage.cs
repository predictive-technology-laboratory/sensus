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
    /// <summary>
    /// Displays the progress of sharing a local data store. Data must be collected and written to a file
    /// that is shared, and this could take some time. This gives the user something to look at while
    /// these things happen.
    /// </summary>
    public class ShareLocalDataStorePage : ContentPage
    {
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensusUI.ShareLocalDataStorePage"/> class.
        /// </summary>
        /// <param name="localDataStore">Local data store to display.</param>
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
                                      
            new Thread(() =>
                {                    
                    string sharePath = SensusServiceHelper.Get().GetSharePath(".json");
                    bool errorWritingShareFile = false;
                    try
                    {     
                        // step 1:  gather data.
                        Device.BeginInvokeOnMainThread(() => statusLabel.Text = "Gathering data...");
                        List<Datum> localData = localDataStore.GetDataForRemoteDataStore(_cancellationTokenSource.Token, progress =>
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                    {
                                        progressBar.ProgressTo(progress, 250, Easing.Linear);
                                    });
                            });                                

                        // step 2:  write gathered data to file.
                        if (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                                {
                                    progressBar.ProgressTo(0, 0, Easing.Linear);
                                    statusLabel.Text = "Writing data to file...";
                                });
                            
                            using (StreamWriter shareFile = new StreamWriter(sharePath))
                            {
                                shareFile.WriteLine("[");

                                int dataWritten = 0;
                                foreach (Datum localDatum in localData)
                                {
                                    if (_cancellationTokenSource.IsCancellationRequested)
                                        break;
                                    
                                    shareFile.Write((dataWritten++ == 0 ? "" : "," + Environment.NewLine) + localDatum.GetJSON(localDataStore.Protocol.JsonAnonymizer));

                                    if (localData.Count >= 10 && (dataWritten % (localData.Count / 10)) == 0)
                                        Device.BeginInvokeOnMainThread(() => progressBar.ProgressTo(dataWritten / (double)localData.Count, 250, Easing.Linear));
                                }

                                shareFile.WriteLine(Environment.NewLine + "]");
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
                    }

                    if (_cancellationTokenSource.IsCancellationRequested || errorWritingShareFile)
                    {
                        // always delete the file on cancel / error
                        try
                        {
                            File.Delete(sharePath);
                        }
                        catch (Exception)
                        {
                        }

                        // the only way to get a cancellation event is to back out of the window, so only pop if there was an error
                        if(errorWritingShareFile)
                            Device.BeginInvokeOnMainThread(async () => await Navigation.PopAsync());
                    }
                    else
                    {
                        Device.BeginInvokeOnMainThread(async () => 
                            {
                                await Navigation.PopAsync();
                                SensusServiceHelper.Get().ShareFileAsync(sharePath, "Sensus Data", "application/json");
                            });
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