﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
                string sharePath = null;
                bool errorWritingShareFile = false;
                try
                {
                    sharePath = SensusServiceHelper.Get().GetSharePath(".zip");

                    int numDataWritten = localDataStore.WriteDataToZipFile(sharePath, _cancellationTokenSource.Token, (message, progress) =>
                    {
                        SensusServiceHelper.Get().MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            uint duration = 250;

                            if (message != null)
                            {
                                statusLabel.Text = message;
                                duration = 0;
                            }

                            await progressBar.ProgressTo(progress, duration, Easing.Linear);
                        });
                    });

                    if (numDataWritten == 0)
                        throw new Exception("No data to share.");
                }
                catch (Exception ex)
                {
                    errorWritingShareFile = true;
                    string message = "Error sharing data:  " + ex.Message;
                    SensusServiceHelper.Get().FlashNotificationAsync(message);
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                }

                if (_cancellationTokenSource.IsCancellationRequested || errorWritingShareFile)
                {
                    // always delete the file on cancel / error
                    try
                    {
                        if (File.Exists(sharePath))
                            File.Delete(sharePath);
                    }
                    catch (Exception)
                    {
                    }

                    // if the window has already been popped then the token will have been cancelled. pop the window if needed.
                    if (!_cancellationTokenSource.IsCancellationRequested)
                        SensusServiceHelper.Get().MainThreadSynchronizer.ExecuteThreadSafe(async () => await Navigation.PopAsync());
                }
                else
                {
                    SensusServiceHelper.Get().MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                    {
                        await Navigation.PopAsync();
                        SensusServiceHelper.Get().ShareFileAsync(sharePath, "Sensus Data:  " + localDataStore.Protocol.Name, "application/zip");
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