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
        public static Task<bool> AuthenticateProtocolAsync(Protocol protocol)
        {
            return Task.Run(async () =>
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
            });
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

                actions.Add(selectedProtocol.Running ? "Stop" : "Start");

                if (!selectedProtocol.Running && selectedProtocol.AllowParticipantIdReset)
                {
                    actions.Add("Reset ID");
                }

                if (!string.IsNullOrWhiteSpace(selectedProtocol.ContactEmail))
                {
                    actions.Add("Email Study Manager for Help");
                }

                if (selectedProtocol.Running && selectedProtocol.AllowViewStatus)
                {
                    actions.Add("Status");
                }

                if (selectedProtocol.AllowViewData)
                {
                    actions.Add("View Data");
                }

                if (selectedProtocol.Running)
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

                if (!selectedProtocol.Running && selectedProtocol.ScheduledStartCallback != null)
                {
                    actions.Remove("Start");
                    actions.Insert(0, "Cancel Scheduled Start");
                }

                actions.Add("Delete");
                #endregion

                #region process selected protocol action
                string selectedAction = await DisplayActionSheet(selectedProtocol.Name, "Cancel", null, actions.ToArray());

                // must reset the protocol selection manually
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    _protocolsList.SelectedItem = null;
                });

                if (selectedAction == "Start")
                {
                    await selectedProtocol.StartWithUserAgreementAsync(null);
                }
                else if (selectedAction == "Cancel Scheduled Start")
                {
                    if (await DisplayAlert("Confirm Cancel", "Are you sure you want to cancel " + selectedProtocol.Name + "?", "Yes", "No"))
                    {
                        selectedProtocol.CancelScheduledStart();
                    }
                }
                else if (selectedAction == "Stop")
                {
                    if (await DisplayAlert("Confirm Stop", "Are you sure you want to stop " + selectedProtocol.Name + "?", "Yes", "No"))
                    {
                        await selectedProtocol.StopAsync();
                    }
                }
                else if (selectedAction == "Reset ID")
                {
                    selectedProtocol.ParticipantId = null;
                    await SensusServiceHelper.Get().FlashNotificationAsync("Your ID has been reset.");
                }
                else if (selectedAction == "Status")
                {
                    List<Tuple<string, Dictionary<string, string>>> events = await selectedProtocol.TestHealthAsync(true);
                    await Navigation.PushAsync(new ViewTextLinesPage("Status", events.SelectMany(healthEventNameProperties =>
                    {
                        return healthEventNameProperties.Item2.Select(propertyValue => healthEventNameProperties.Item1 + ":  " + propertyValue.Key + "=" + propertyValue.Value);

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
                        if (await selectedProtocol.RemoteDataStore?.WriteLocalDataStoreAsync(CancellationToken.None))
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Data submitted.");
                        }
                        else
                        {
                            throw new Exception("Failed to submit data.");
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
                            ParticipationRewardDatum participationRewardDatum = new ParticipationRewardDatum(DateTimeOffset.UtcNow, selectedProtocol.Participation);
                            participationRewardDatum.ProtocolId = selectedProtocol.Id;
                            participationRewardDatum.ParticipantId = selectedProtocol.ParticipantId;

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
                    Input input = await SensusServiceHelper.Get().PromptForInputAsync("Group",
                        new ItemPickerPageInput("Select Protocols", groupableProtocols.Cast<object>().ToList(), "Name")
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
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedProtocol.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        await selectedProtocol.DeleteAsync();
                    }
                }
            };
            #endregion

            Content = _protocolsList;

            #region add toolbar items
            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
            {
                List<string> buttons = new string[] { "From QR Code", "From Account QR Code", "From URL", "New" }.ToList();

                string action = await DisplayActionSheet("Add Study", "Back", null, buttons.ToArray());
                string protocolId = null;
                Protocol protocol = null;

                if (action == "From QR Code")
                {
                    string result = await SensusServiceHelper.Get().ScanQrCodeAsync(QrCodePrefix.SENSUS_PROTOCOL);

                    if (result != null)
                    {
                        try
                        {
                            protocol = await Protocol.DeserializeAsync(new Uri(result));
                            protocol.LastProtocolURL = result;
                        }
                        catch (Exception ex)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Failed to get study from QR code:  " + ex.Message);
                        }
                    }
                }
                if (action == "From Account QR Code")
                {
                    string result = await SensusServiceHelper.Get().ScanQrCodeAsync(QrCodePrefix.IAM_CREDENTIALS);

                    if (result != null)
                    {
                        try
                        {
                            var split = result.Contains(":") ? result.Split(':') : new[] { result };
                            var baseUrl = split[0];
                            var participantId = split.Length > 1 ? split[1] : null;
                            var deviceId = SensusServiceHelper.Get().DeviceId;
                            var accountService = new AccountService(baseUrl);
                            var account = await accountService.GetAccount(deviceId, participantId);
                            if(string.IsNullOrWhiteSpace(account?.protocolURL))
                            {
                                throw new Exception("Account did not return a valid Protocol URL");
                            }
                            protocolId = account.protocolId;
                            protocol = await Protocol.DeserializeAsync(new Uri(account.protocolURL));
                            protocol.ParticipantId = participantId;
                            protocol.LastProtocolURL = account?.protocolURL;
                            if (string.IsNullOrWhiteSpace(protocolId) == false && protocolId != protocol.Id)
                            {
                                throw new Exception("The Id on the returned protocol does not match the expected Id");
                            }
                        }
                        catch (Exception ex)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Failed to get study from Account QR code:  " + ex.Message);
                        }
                    }
                }
                else if (action == "From URL")
                {
                    Input input = await SensusServiceHelper.Get().PromptForInputAsync("Download Protocol", new SingleLineTextInput("Protocol URL:", Keyboard.Url), null, true, null, null, null, null, false);

                    // input might be null (user cancelled), or the value might be null (blank input submitted)
                    if (!string.IsNullOrEmpty(input?.Value?.ToString()))
                    {
                        try
                        {
                            protocol = await Protocol.DeserializeAsync(new Uri(input.Value.ToString()));
                            protocol.LastProtocolURL = input.Value.ToString();
                        }
                        catch(Exception ex)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Failed to get study from URL:  " + ex.Message);
                        }
                    }
                }
                else if (action == "New")
                {
                    Protocol.Create("New Protocol");
                }

                if (protocol != null)
                {
                    var existingProtocol = SensusServiceHelper.Get()?.RegisteredProtocols.FirstOrDefault(w => w.Id == protocol.Id);
                    if(existingProtocol != null)
                    {
                        if (existingProtocol.Running)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync($"The new study is the same as {protocol.Name} which is running.  Please stop the {protocol.Name} study and try again. ");
                            protocol = null;  //don't replace a running protocol
                        }
                        else
                        {
                            await existingProtocol.DeleteAsync(); //remove the existing protocol before we add it back
                        }
                    }
                }
                if(protocol != null)
                {
                    await Protocol.DisplayAndStartAsync(protocol); //TODO:  I am not seeing where, but this must add add the protocol to the registered list
                }
            }));

            ToolbarItems.Add(new ToolbarItem("ID", null, async () =>
            {
                await DisplayAlert("Device ID", SensusServiceHelper.Get().DeviceId, "Close");

            }, ToolbarItemOrder.Secondary));

            ToolbarItems.Add(new ToolbarItem("Log", null, async () =>
            {
                await Navigation.PushAsync(new ViewTextLinesPage("Log", SensusServiceHelper.Get().Logger.Read(200, true), () => SensusServiceHelper.Get().Logger.Clear()));

            }, ToolbarItemOrder.Secondary));

#if __ANDROID__
            ToolbarItems.Add(new ToolbarItem("Stop", null, async () =>
            {
                if (await DisplayAlert("Confirm", "Are you sure you want to stop Sensus? This will end your participation in all studies.", "Stop Sensus", "Go Back"))
                {
                    SensusServiceHelper.Get().StopProtocols();

                    (SensusServiceHelper.Get() as Android.IAndroidSensusServiceHelper)?.StopAndroidSensusService();
                }

            }, ToolbarItemOrder.Secondary));
#endif

            ToolbarItems.Add(new ToolbarItem("About", null, async () =>
            {
                await DisplayAlert("About Sensus", "Version:  " + SensusServiceHelper.Get().Version, "OK");

            }, ToolbarItemOrder.Secondary));
            #endregion
        }
    }
}