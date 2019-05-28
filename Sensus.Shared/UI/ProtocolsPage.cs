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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Xamarin.Forms;
using Sensus.Context;
using Sensus.UI.Inputs;
using System.Threading.Tasks;
using Sensus.Authentication;
using System.Net;
using Sensus.Notifications;
using System.Text;

#if __ANDROID__
using Sensus.Android;
#endif

namespace Sensus.UI
{
    /// <summary>
    /// Displays all protocols.
    /// </summary>
    public class ProtocolsPage : ContentPage
    {
        public static async Task<bool> AuthenticateProtocolAsync(Protocol protocol)
        {
            if (protocol.LockPasswordHash == "")
            {
                return true;
            }
            else
            {
                Input input = await SensusServiceHelper.Get().PromptForInputAsync("Authenticate \"" + protocol.Name + "\"", new SingleLineTextInput("Protocol Password:", Keyboard.Text, true), null, true, null, null, null, null, false);

                if (input == null)
                {
                    return false;
                }
                else
                {
                    string password = input.Value as string;

                    if (password != null && SensusServiceHelper.Get().GetHash(password) == protocol.LockPasswordHash)
                    {
                        return true;
                    }
                    else
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("The password you entered was not correct.");
                        return false;
                    }
                }
            }
        }

        private ListView _protocolsList;

        public ProtocolsPage()
        {
            Title = "Your Studies";

            _protocolsList = new ListView(ListViewCachingStrategy.RecycleElement);
            _protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(Protocol.Caption));
            _protocolsList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(Protocol.SubCaption));
            _protocolsList.ItemsSource = SensusServiceHelper.Get()?.RegisteredProtocols;
            _protocolsList.ItemTapped += async (o, e) =>
            {
                if (_protocolsList.SelectedItem == null)
                {
                    return;
                }

                Protocol selectedProtocol = _protocolsList.SelectedItem as Protocol;

                #region add protocol actions
                List<string> actions = new List<string>();

                if (selectedProtocol.State == ProtocolState.Running)
                {
                    actions.Add("Stop");

                    if (selectedProtocol.AllowPause)
                    {
                        actions.Add("Pause");
                    }
                }
                else if (selectedProtocol.State == ProtocolState.Stopped)
                {
                    actions.Add("Start");
                }
                else if (selectedProtocol.State == ProtocolState.Paused)
                {
                    actions.Add("Resume");
                }

                if (selectedProtocol.AllowTagging)
                {
                    actions.Add("Tag Data");
                }

                if (selectedProtocol.State == ProtocolState.Stopped && selectedProtocol.AllowParticipantIdReset)
                {
                    actions.Add("Reset ID");
                }

                if (!string.IsNullOrWhiteSpace(selectedProtocol.ContactEmail))
                {
                    actions.Add("Email Study Manager for Help");
                }

                if (selectedProtocol.State == ProtocolState.Running && selectedProtocol.AllowViewStatus)
                {
                    actions.Add("Status");
                }

                if (selectedProtocol.AllowViewData)
                {
                    actions.Add("View Data");
                }

                if (selectedProtocol.State == ProtocolState.Running)
                {
                    if (selectedProtocol.AllowSubmitData)
                    {
                        actions.Add("Submit Data");
                    }

                    if (selectedProtocol.AllowParticipationScanning)
                    {
                        actions.Add("Display Participation");
                    }
                }

                if (selectedProtocol.AllowParticipationScanning && (selectedProtocol.RemoteDataStore?.CanRetrieveWrittenData ?? false))
                {
                    actions.Add("Scan Participation Barcode");
                }

                actions.Add("Edit");

                if (selectedProtocol.AllowCopy)
                {
                    actions.Add("Copy");
                }

                if (selectedProtocol.Shareable)
                {
                    actions.Add("Share Protocol");
                }

                if (selectedProtocol.AllowLocalDataShare && (selectedProtocol.LocalDataStore?.HasDataToShare ?? false))
                {
                    actions.Add("Share Local Data");
                }

                List<Protocol> groupableProtocols = SensusServiceHelper.Get().RegisteredProtocols.Where(registeredProtocol => registeredProtocol != selectedProtocol && registeredProtocol.Groupable && registeredProtocol.GroupedProtocols.Count == 0).ToList();
                if (selectedProtocol.Groupable)
                {
                    if (selectedProtocol.GroupedProtocols.Count == 0 && groupableProtocols.Count > 0)
                    {
                        actions.Add("Group");
                    }
                    else if (selectedProtocol.GroupedProtocols.Count > 0)
                    {
                        actions.Add("Ungroup");
                    }
                }

                if (selectedProtocol.State == ProtocolState.Stopped && selectedProtocol.StartIsScheduled)
                {
                    actions.Remove("Start");
                    actions.Insert(0, "Cancel Scheduled Start");
                }

                if (selectedProtocol.State == ProtocolState.Running && selectedProtocol.AllowTestPushNotification)
                {
                    actions.Add("Request Test Push Notification");
                }

                actions.Add("Delete");
                #endregion

                #region process selected protocol action
                string selectedAction = await DisplayActionSheet(selectedProtocol.Name, "Cancel", null, actions.ToArray());

                // must reset the protocol selection manually
                _protocolsList.SelectedItem = null;

                if (selectedAction == "Start")
                {
                    await selectedProtocol.StartWithUserAgreementAsync();
                }
                else if (selectedAction == "Cancel Scheduled Start")
                {
                    if (await DisplayAlert("Confirm Cancel", "Are you sure you want to cancel " + selectedProtocol.Name + "?", "Yes", "No"))
                    {
                        await selectedProtocol.CancelScheduledStartAsync();
                    }
                }
                else if (selectedAction == "Stop")
                {
                    if (await DisplayAlert("Confirm Stop", "Are you sure you want to stop " + selectedProtocol.Name + "?", "Yes", "No"))
                    {
                        await selectedProtocol.StopAsync();
                    }
                }
                else if (selectedAction == "Pause")
                {
                    await selectedProtocol.PauseAsync();
                }
                else if (selectedAction == "Resume")
                {
                    await selectedProtocol.ResumeAsync();
                }
                else if (selectedAction == "Tag Data")
                {
                    await Navigation.PushAsync(new TaggingPage(selectedProtocol));
                }
                else if (selectedAction == "Reset ID")
                {
                    selectedProtocol.ParticipantId = null;
                    await SensusServiceHelper.Get().FlashNotificationAsync("Your ID has been reset.");
                }
                else if (selectedAction == "Status")
                {
                    List<AnalyticsTrackedEvent> trackedEvents = await selectedProtocol.TestHealthAsync(true, CancellationToken.None);
                    await Navigation.PushAsync(new ViewTextLinesPage("Status", trackedEvents.SelectMany(trackedEvent =>
                    {
                        return trackedEvent.Properties.Select(propertyValue => trackedEvent.Name + ":  " + propertyValue.Key + "=" + propertyValue.Value);

                    }).ToList()));
                }
                else if (selectedAction == "Email Study Manager for Help")
                {
                    await SensusServiceHelper.Get().SendEmailAsync(selectedProtocol.ContactEmail, "Help with Sensus study:  " + selectedProtocol.Name,
                        "Hello - " + Environment.NewLine +
                        Environment.NewLine +
                        "I am having trouble with a Sensus study. The name of the study is \"" + selectedProtocol.Name + "\"." + Environment.NewLine +
                        Environment.NewLine +
                        "Here is why I am sending this email:  ");
                }
                else if (selectedAction == "View Data")
                {
                    await Navigation.PushAsync(new ProbesViewPage(selectedProtocol));
                }
                else if (selectedAction == "Submit Data")
                {
                    try
                    {
						if (await ConfirmSubmission(selectedProtocol))
						{
							if (await selectedProtocol.RemoteDataStore?.WriteLocalDataStoreAsync(CancellationToken.None, true))
							{
								await SensusServiceHelper.Get().FlashNotificationAsync("Data submitted.");
							}
							else
							{
								throw new Exception("Failed to submit data.");
							}
						}
                    }
                    catch (Exception ex)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Error:  " + ex.Message);
                    }
                }
                else if (selectedAction == "Display Participation")
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                    // pop up wait screen while we submit the participation reward datum
                    IEnumerable<InputGroup> inputGroups = await SensusServiceHelper.Get().PromptForInputsAsync(
                        null,
                        new InputGroup[] { new InputGroup { Name = "Please Wait", Inputs = { new LabelOnlyInput("Submitting participation information.", false) } } },
                        cancellationTokenSource.Token,
                        false,
                        "Cancel",
                        null,
                        null,
                        null,
                        false,
                        async () =>
                        {
                            ParticipationRewardDatum participationRewardDatum = new ParticipationRewardDatum(DateTimeOffset.UtcNow, selectedProtocol.Participation)
                            {
                                ProtocolId = selectedProtocol.Id,
                                ParticipantId = selectedProtocol.ParticipantId
                            };

                            bool writeFailed = false;
                            try
                            {
                                await selectedProtocol.RemoteDataStore.WriteDatumAsync(participationRewardDatum, cancellationTokenSource.Token);
                            }
                            catch (Exception)
                            {
                                writeFailed = true;
                            }

                            if (writeFailed)
                            {
                                await SensusServiceHelper.Get().FlashNotificationAsync("Failed to submit participation information to remote server. You will not be able to verify your participation at this time.");
                            }

                            // cancel the token to close the input above, but only if the token hasn't already been canceled.
                            if (!cancellationTokenSource.IsCancellationRequested)
                            {
                                cancellationTokenSource.Cancel();
                            }

                            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                            {
                                // only show the QR code for the reward datum if the datum was written to the remote data store and if the data store can retrieve it.
                                await Navigation.PushAsync(new ParticipationReportPage(selectedProtocol, participationRewardDatum, !writeFailed && (selectedProtocol.RemoteDataStore?.CanRetrieveWrittenData ?? false)));
                            });
                        });

                    // if the prompt was closed by the user instead of the cancellation token, cancel the token in order
                    // to cancel the remote data store write. if the prompt was closed by the termination of the remote
                    // data store write (i.e., by the canceled token), then don't cancel the token again.
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                    }
                }
                else if (selectedAction == "Scan Participation Barcode")
                {
                    try
                    {
                        string barcodeResult = await SensusServiceHelper.Get().ScanQrCodeAsync(QrCodePrefix.SENSUS_PARTICIPATION);

                        if (barcodeResult == null)
                        {
                            return;
                        }

                        await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                            // pop up wait screen while we get the participation reward datum
                            IEnumerable<InputGroup> inputGroups = await SensusServiceHelper.Get().PromptForInputsAsync(
                                null,
                                new InputGroup[] { new InputGroup { Name = "Please Wait", Inputs = { new LabelOnlyInput("Retrieving participation information.", false) } } },
                                cancellationTokenSource.Token,
                                false,
                                "Cancel",
                                null,
                                null,
                                null,
                                false,
                                async () =>
                                {
                                    // after the page shows up, attempt to retrieve the participation reward datum.
                                    try
                                    {
                                        ParticipationRewardDatum participationRewardDatum = await selectedProtocol.RemoteDataStore.GetDatumAsync<ParticipationRewardDatum>(barcodeResult, cancellationTokenSource.Token);

                                        // cancel the token to close the input above, but only if the token hasn't already been canceled by the user.
                                        if (!cancellationTokenSource.IsCancellationRequested)
                                        {
                                            cancellationTokenSource.Cancel();
                                        }

                                        // ensure that the participation datum has not expired                                           
                                        if (participationRewardDatum.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-SensusServiceHelper.PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS))
                                        {
                                            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                                            {
                                                await Navigation.PushAsync(new VerifiedParticipationPage(selectedProtocol, participationRewardDatum));
                                            });
                                        }
                                        else
                                        {
                                            await SensusServiceHelper.Get().FlashNotificationAsync("Participation barcode has expired. The participant needs to regenerate the barcode.");
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        await SensusServiceHelper.Get().FlashNotificationAsync("Failed to retrieve participation information.");
                                    }
                                    finally
                                    {
                                        // cancel the token to close the input above, but only if the token hasn't already been canceled by the user. this will be
                                        // used if an exception is thrown while getting the participation reward datum.
                                        if (!cancellationTokenSource.IsCancellationRequested)
                                        {
                                            cancellationTokenSource.Cancel();
                                        }
                                    }
                                });

                            // if the prompt was closed by the user instead of the cancellation token, cancel the token in order
                            // to cancel the datum retrieval. if the prompt was closed by the termination of the remote
                            // data store get (i.e., by the canceled token), then don't cancel the token again.
                            if (!cancellationTokenSource.IsCancellationRequested)
                            {
                                cancellationTokenSource.Cancel();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        string message = "Failed to scan barcode:  " + ex.Message;
                        SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                        await SensusServiceHelper.Get().FlashNotificationAsync(message);
                    }
                }
                else if (selectedAction == "Edit")
                {
                    if (await AuthenticateProtocolAsync(selectedProtocol))
                    {
                        await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            ProtocolPage protocolPage = new ProtocolPage(selectedProtocol);
                            await Navigation.PushAsync(protocolPage);
                        });
                    }
                }
                else if (selectedAction == "Copy")
                {
                    // reset the protocol id, as we're creating a new study
                    await selectedProtocol.CopyAsync(true, true);
                }
                else if (selectedAction == "Share Protocol")
                {
                    await selectedProtocol.ShareAsync();
                }
                else if (selectedAction == "Share Local Data")
                {
                    await selectedProtocol.LocalDataStore?.ShareLocalDataAsync();
                }
                else if (selectedAction == "Group")
                {
                    Input input = await SensusServiceHelper.Get().PromptForInputAsync("Group", new ItemPickerPageInput("Select Protocols", groupableProtocols.Cast<object>().ToList(), textBindingPropertyPath: nameof(Protocol.Name))
                    {
                        Multiselect = true

                    }, null, true, "Group", null, null, null, false);

                    if (input == null)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("No protocols grouped.");
                        return;
                    }

                    ItemPickerPageInput itemPickerPageInput = input as ItemPickerPageInput;

                    List<Protocol> selectedProtocols = (itemPickerPageInput.Value as List<object>).Cast<Protocol>().ToList();

                    if (selectedProtocols.Count == 0)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("No protocols grouped.");
                    }
                    else
                    {
                        selectedProtocol.GroupedProtocols.AddRange(selectedProtocols);
                        await SensusServiceHelper.Get().FlashNotificationAsync("Grouped \"" + selectedProtocol.Name + "\" with " + selectedProtocols.Count + " other protocol" + (selectedProtocols.Count == 1 ? "" : "s") + ".");
                    }
                }
                else if (selectedAction == "Ungroup")
                {
                    if (await DisplayAlert("Ungroup " + selectedProtocol.Name + "?", "This protocol is currently grouped with the following other protocols:" + Environment.NewLine + Environment.NewLine + string.Concat(selectedProtocol.GroupedProtocols.Select(protocol => protocol.Name + Environment.NewLine)), "Ungroup", "Cancel"))
                    {
                        selectedProtocol.GroupedProtocols.Clear();
                    }
                }
                else if (selectedAction == "Request Test Push Notification")
                {
                    try
                    {
                        PushNotificationRequest request = new PushNotificationRequest(SensusServiceHelper.Get().DeviceId + ".test", SensusServiceHelper.Get().DeviceId, selectedProtocol, "Test", "Your test push notification has been delivered.", "default", PushNotificationRequest.LocalFormat, DateTimeOffset.UtcNow, Guid.NewGuid());
                        await SensusContext.Current.Notifier.SendPushNotificationRequestAsync(request, CancellationToken.None);
                        await DisplayAlert("Pending", "Your test push notification was sent and is pending delivery. It should come back within 5 minutes.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", "Failed to send test push notification:  " + ex.Message, "OK");
                    }
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedProtocol.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        await selectedProtocol.DeleteAsync();
                    }
                }
                #endregion
            };

            Content = _protocolsList;

            #region add toolbar items
            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
            {
                string action = await DisplayActionSheet("Add Study", "Back", null, new[] { "From QR Code", "From URL", "New" });

                if (action == "New")
                {
                    await Protocol.CreateAsync("New Study");
                }
                else
                {
                    string url = null;

                    if (action == "From QR Code")
                    {
                        url = await SensusServiceHelper.Get().ScanQrCodeAsync(QrCodePrefix.SENSUS_PROTOCOL);
                    }
                    else if (action == "From URL")
                    {
                        Input input = await SensusServiceHelper.Get().PromptForInputAsync("Download Study", new SingleLineTextInput("Study URL:", Keyboard.Url), null, true, null, null, null, null, false);

                        // input might be null (user cancelled), or the value might be null (blank input submitted)
                        url = input?.Value?.ToString();
                    }

                    if (url != null)
                    {
                        Protocol protocol = null;
                        Exception loadException = null;

                        // handle managed studies...handshake with authentication service.
                        if (url.StartsWith(Protocol.MANAGED_URL_STRING))
                        {
                            ProgressPage loadProgressPage = null;

                            try
                            {
                                Tuple<string, string> baseUrlParticipantId = ParseManagedProtocolURL(url);

                                AuthenticationService authenticationService = new AuthenticationService(baseUrlParticipantId.Item1);

                                // get account and credentials. this can take a while, so show the user something fun to look at.
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                                loadProgressPage = new ProgressPage("Configuring study. Please wait...", cancellationTokenSource);
                                await loadProgressPage.DisplayAsync(Navigation);

                                await loadProgressPage.SetProgressAsync(0, "creating account");
                                Account account = await authenticationService.CreateAccountAsync(baseUrlParticipantId.Item2);
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                                await loadProgressPage.SetProgressAsync(0.3, "getting credentials");
                                AmazonS3Credentials credentials = await authenticationService.GetCredentialsAsync();
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                                await loadProgressPage.SetProgressAsync(0.6, "downloading study");
                                protocol = await Protocol.DeserializeAsync(new Uri(credentials.ProtocolURL), true, credentials);
                                await loadProgressPage.SetProgressAsync(1, null);

                                // don't throw for cancellation here as doing so will leave the protocol partially configured. if 
                                // the download succeeds, ensure that the properties get set below before throwing any exceptions.
                                protocol.ParticipantId = account.ParticipantId;
                                protocol.AuthenticationService = authenticationService;

                                // make sure protocol has the id that we expect
                                if (protocol.Id != credentials.ProtocolId)
                                {
                                    throw new Exception("The identifier of the study does not match that of the credentials.");
                                }
                            }
                            catch (Exception ex)
                            {
                                loadException = ex;
                            }
                            finally
                            {
                                // ensure the progress page is closed
                                await (loadProgressPage?.CloseAsync() ?? Task.CompletedTask);
                            }
                        }
                        // handle unmanaged studies...direct download from URL.
                        else
                        {
                            try
                            {
                                protocol = await Protocol.DeserializeAsync(new Uri(url), true);
                            }
                            catch (Exception ex)
                            {
                                loadException = ex;
                            }
                        }

                        // show load exception to user
                        if (loadException != null)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Failed to get study:  " + loadException.Message);
                            protocol = null;
                        }

                        // start protocol if we have one
                        if (protocol != null)
                        {
                            // save app state to hang on to protocol, authentication information, etc.
                            await SensusServiceHelper.Get().SaveAsync();

                            // show the protocol to the user and start
                            await Protocol.DisplayAndStartAsync(protocol);
                        }
                    }
                }
            }));

            ToolbarItems.Add(new ToolbarItem("ID", null, async () =>
            {
                await DisplayAlert("Device ID", SensusServiceHelper.Get().DeviceId, "Close");

            }, ToolbarItemOrder.Secondary));

            ToolbarItems.Add(new ToolbarItem("Log", null, async () =>
            {
                Logger logger = SensusServiceHelper.Get().Logger as Logger;
                await Navigation.PushAsync(new ViewTextLinesPage("Log", logger.Read(500, true), logger.Clear));

            }, ToolbarItemOrder.Secondary));

#if __ANDROID__
            ToolbarItems.Add(new ToolbarItem("Stop", null, async () =>
            {
                if (await DisplayAlert("Confirm", "Are you sure you want to stop Sensus? This will end your participation in all studies.", "Stop Sensus", "Go Back"))
                {
                    // stop all protocols and then stop the service. stopping the service alone does not stop 
                    // the service, as we want to cover the case when the os stops/destroys the service. in this
                    // case we do not want to mark the protocols as stopped, as we'd like them to start back
                    // up when the os (or a push notification) starts the service again.
                    await SensusServiceHelper.Get().StopAsync();

                    global::Android.App.Application.Context.StopService(AndroidSensusService.GetServiceIntent(false));
                }

            }, ToolbarItemOrder.Secondary));
#endif

            ToolbarItems.Add(new ToolbarItem("About", null, async () =>
            {
                await DisplayAlert("About Sensus", "Version:  " + SensusServiceHelper.Get().Version, "OK");

            }, ToolbarItemOrder.Secondary));
            #endregion
        }

		public async Task<bool> ConfirmSubmission(Protocol selectedProtocol)
		{
			StringBuilder sb = new StringBuilder();
			bool submit = true;

			sb.AppendLine("The following requirements for submitting this protocol are not met:");
			sb.AppendLine();

			if (selectedProtocol.RemoteDataStore.RequireWiFi && !SensusServiceHelper.Get().WiFiConnected)
			{
				sb.AppendLine("Wifi is not connected.");

				submit = false;
			}

			if (selectedProtocol.RemoteDataStore.RequireCharging && !SensusServiceHelper.Get().IsCharging)
			{
				sb.AppendLine("The device is not charging.");

				submit = false;

			}

			if (selectedProtocol.RemoteDataStore.RequiredBatteryChargeLevelPercent > 0 && SensusServiceHelper.Get().BatteryChargePercent < selectedProtocol.RemoteDataStore.RequiredBatteryChargeLevelPercent)
			{
				sb.AppendLine($"The battery charge is less than {selectedProtocol.RemoteDataStore.RequiredBatteryChargeLevelPercent}%.");

				submit = false;
			}

			if (submit == false)
			{
				sb.AppendLine("");
				sb.AppendLine("Do you still want to submit?");

				submit = await DisplayAlert("Submit?", sb.ToString(), "Yes", "No");
			}

			return submit;
		}

        private Tuple<string, string> ParseManagedProtocolURL(string url)
        {
            if (!url.StartsWith(Protocol.MANAGED_URL_STRING))
            {
                throw new Exception("Study URL is not managed.");
            }

            // should have the following parts (participant is optional but the last colon is still required):  managed:BASEURL:PARTICIPANT_ID
            int firstColon = url.IndexOf(':');
            int lastColon = url.LastIndexOf(':');

            if (firstColon == lastColon)
            {
                throw new Exception("Invalid managed study URL format.");
            }

            string baseUrl = url.Substring(firstColon + 1, lastColon - firstColon - 1);

            // get participant id if one follows the last colon
            string participantId = null;
            if (lastColon < url.Length - 1)
            {
                participantId = url.Substring(lastColon + 1);
            }

            return new Tuple<string, string>(baseUrl, participantId);
        }
    }
}