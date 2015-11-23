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

using SensusService;
using SensusUI.UiProperties;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using SensusUI.Inputs;
using System.Threading.Tasks;
using ZXing;

namespace SensusUI
{
    /// <summary>
    /// First thing the user sees.
    /// </summary>
    public class SensusMainPage : ContentPage
    {
        public SensusMainPage()
        {
            Title = "Sensus";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            #region protocols

            Button viewProtocolsButton = new Button
            {
                Text = "View Protocols",
                FontSize = 20
            };

            viewProtocolsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ProtocolsPage());
            };

            contentLayout.Children.Add(viewProtocolsButton);

            #endregion

            #region show participation

            Button showParticipationButton = new Button
            {
                Text = "Show Participation",
                FontSize = 20
            };

            showParticipationButton.Clicked += async (o, e) =>
            {
                Protocol selectedProtocol = await SelectProtocol(true);

                if (selectedProtocol == null)
                    return;

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                // pop up wait screen while we submit the participation reward datum
                SensusServiceHelper.Get().PromptForInputsAsync(
                    null,
                    false,
                    DateTime.MinValue,
                    new InputGroup[]
                    {
                        new InputGroup("Please Wait", new LabelOnlyInput("Submitting participation information.", false))
                    },
                    cancellationTokenSource.Token,
                    false,
                    "Cancel",
                    () =>
                    {
                        // add participation reward datum to remote data store and commit immediately
                        ParticipationRewardDatum participationRewardDatum = new ParticipationRewardDatum(DateTimeOffset.UtcNow, selectedProtocol.Participation);
                        selectedProtocol.RemoteDataStore.AddNonProbeDatum(participationRewardDatum);
                        selectedProtocol.RemoteDataStore.CommitAsync(cancellationTokenSource.Token, true, async () =>
                            {
                                // we should not have any remaining non-probe data
                                bool commitFailed = selectedProtocol.RemoteDataStore.HasNonProbeDatumToCommit(participationRewardDatum.Id);

                                if (commitFailed)
                                    SensusServiceHelper.Get().FlashNotificationAsync("Failed to submit participation information to remote server. You will not be able to verify your participation at this time.");

                                // cancel the token to close the input above, but only if the token hasn't already been canceled.
                                if (!cancellationTokenSource.IsCancellationRequested)
                                    cancellationTokenSource.Cancel();

                                Device.BeginInvokeOnMainThread(async() =>
                                    {
                                        // only show the QR code for the reward datum if the datum was committed to the remote data store
                                        await Navigation.PushAsync(new ParticipationReportPage(selectedProtocol, commitFailed ? null : participationRewardDatum));
                                    });
                            });
                    },
                    inputs =>
                    {
                        // if the prompt was closed by the user instead of the cancellation token, cancel the token in order
                        // to cancel the remote data store commit. if the prompt was closed by the termination of the remote
                        // data store commit (i.e., by the canceled token), then don't cancel the token again.
                        if (!cancellationTokenSource.IsCancellationRequested)
                            cancellationTokenSource.Cancel();
                    });                                
            };

            contentLayout.Children.Add(showParticipationButton);

            #endregion

            #region verify participation

            Button verifyParticipationButton = new Button
            {
                Text = "Verify Participation",
                FontSize = 20
            };

            verifyParticipationButton.Clicked += async (o, e) =>
            {
                Protocol selectedProtocol = await SelectProtocol(false);

                if (selectedProtocol == null)
                    return;

                Result barcodeResult = null;

                try
                {
                    ZXing.Mobile.MobileBarcodeScanner scanner = SensusServiceHelper.Get().BarcodeScanner;

                    if (scanner == null)
                        throw new Exception("Barcode scanner not present.");
                        
                    scanner.TopText = "Position a Sensus participation barcode in the window below, with the red line across the middle of the barcode.";
                    scanner.BottomText = "Sensus is not recording any of these images. Sensus is only trying to find a barcode.";
                    scanner.CameraUnsupportedMessage = "There is not a supported camera on this phone. Cannot scan barcode.";

                    barcodeResult = await scanner.Scan(new ZXing.Mobile.MobileBarcodeScanningOptions
                        {
                            PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE }.ToList()
                        });
                }
                catch (Exception ex)
                {
                    string message = "Failed to scan barcode:  " + ex.Message;
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                    SensusServiceHelper.Get().FlashNotificationAsync(message);
                }

                if (barcodeResult != null)
                {
                    // the barcode activity has just covered sensus, so the service helper is currently unset. if we directly call
                    // UiBoundSensusServiceHelper.Get below, we'll block the UI thread and prevent sensus from resuming and setting
                    // the service helper (deadlock). instead, run on a new thread so that sensus can resume and set the service helper.
                    new Thread(() =>
                        {
                            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                            // pop up wait screen while we get the participation reward datum
                            SensusServiceHelper.Get().PromptForInputsAsync(
                                null,
                                false,
                                DateTime.MinValue,
                                new InputGroup[]
                                {
                                    new InputGroup("Please Wait", new LabelOnlyInput("Retrieving participation information.", false))
                                },
                                cancellationTokenSource.Token,
                                false,
                                "Cancel",
                                async () =>
                                {
                                    try
                                    {
                                        ParticipationRewardDatum participationRewardDatum = await selectedProtocol.RemoteDataStore.GetDatum<ParticipationRewardDatum>(barcodeResult.Text, cancellationTokenSource.Token);

                                        // cancel the token to close the input above, but only if the token hasn't already been canceled.
                                        if (!cancellationTokenSource.IsCancellationRequested)
                                            cancellationTokenSource.Cancel();

                                        // ensure that the participation datum has not expired                                           
                                        if (participationRewardDatum.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-SensusServiceHelper.PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS))
                                        {
                                            Device.BeginInvokeOnMainThread(async() =>
                                                {
                                                    await Navigation.PushAsync(new VerifiedParticipationPage(selectedProtocol, participationRewardDatum));
                                                });
                                        }
                                        else
                                            SensusServiceHelper.Get().FlashNotificationAsync("Participation barcode has expired. The participant needs to regenerate the barcode.");
                                    }
                                    catch (Exception)
                                    {
                                        SensusServiceHelper.Get().FlashNotificationAsync("Failed to retrieve participation information.");
                                    }
                                    finally
                                    {
                                        // cancel the token to close the input above, but only if the token hasn't already been canceled. this will be
                                        // used if an exception is thrown while getting the participation reward datum.
                                        if (!cancellationTokenSource.IsCancellationRequested)
                                            cancellationTokenSource.Cancel();
                                    }                                        
                                },
                                inputs =>
                                {
                                    // if the prompt was closed by the user instead of the cancellation token, cancel the token in order
                                    // to cancel the datum retrieval. if the prompt was closed by the termination of the remote
                                    // data store get (i.e., by the canceled token), then don't cancel the token again.
                                    if (!cancellationTokenSource.IsCancellationRequested)
                                        cancellationTokenSource.Cancel();
                                });  

                        }).Start();
                }
            };

            contentLayout.Children.Add(verifyParticipationButton);

            #endregion

            ToolbarItems.Add(new ToolbarItem(null, "gear_wrench.png", async () =>
                    {
                        string action = await DisplayActionSheet("Other Actions", "Back", null, "View Log", "View Points of Interest", "Stop Sensus");

                        if (action == "View Log")
                            await Navigation.PushAsync(new ViewTextLinesPage("Log", SensusServiceHelper.Get().Logger.Read(200, true), () => SensusServiceHelper.Get().Logger.Clear()));
                        else if (action == "View Points of Interest")
                            await Navigation.PushAsync(new PointsOfInterestPage(SensusServiceHelper.Get().PointsOfInterest, () => SensusServiceHelper.Get().SaveAsync()));
                        else if (action == "Stop Sensus" && await DisplayAlert("Stop Sensus?", "Are you sure you want to stop Sensus?", "OK", "Cancel"))
                            SensusServiceHelper.Get().StopAsync();
                    }));

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }

        private async Task<Protocol> SelectProtocol(bool requireRunning)
        {
            if (SensusServiceHelper.Get().RegisteredProtocols.Count == 0)
            {
                SensusServiceHelper.Get().FlashNotificationAsync("You have not yet added any studies to Sensus.");
                return null;
            }
            
            string[] protocolNames = SensusServiceHelper.Get().RegisteredProtocols.Select((protocol, index) => (index + 1) + ") " + protocol.Name).ToArray();
            string cancelButtonName = "Cancel";
            string selectedProtocolName = await DisplayActionSheet("Select Study", cancelButtonName, null, protocolNames);

            if (!string.IsNullOrWhiteSpace(selectedProtocolName) && selectedProtocolName != cancelButtonName)
            {
                Protocol selectedProtocol = SensusServiceHelper.Get().RegisteredProtocols[int.Parse(selectedProtocolName.Substring(0, selectedProtocolName.IndexOf(")"))) - 1];

                if (!requireRunning || selectedProtocol.Running)
                    return selectedProtocol;
                else if (await DisplayAlert("Begin Study", "You are not currently participating in the \"" + selectedProtocol.Name + "\" study. Would you like to begin participating now?", "Yes", "No"))
                    selectedProtocol.StartWithUserAgreementAsync(null);
            }

            return null;
        }
    }
}