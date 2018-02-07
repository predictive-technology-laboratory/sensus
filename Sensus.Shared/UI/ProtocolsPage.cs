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
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Xamarin.Forms;
using Sensus.Context;
using Sensus.UI.Inputs;
using Sensus.Exceptions;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using ZXing;

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
        private class ProtocolNameValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                Protocol protocol = value as Protocol;
                if (protocol == null)
                    return null;
                else
                    return protocol.Name + " (" + (protocol.Running ? "Running" : (protocol.ScheduledStartCallback != null ? "Scheduled: " + protocol.StartDate.ToShortDateString() + " " + (protocol.StartDate.Date + protocol.StartTime).ToShortTimeString() : "Stopped")) + ")";
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                SensusException.Report("Invalid call to " + GetType().FullName + ".ConvertBack.");
                return null;
            }
        }

        private class ProtocolColorValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                Protocol protocol = value as Protocol;
                if (protocol == null)
                    return Color.Default;
                else
                    return protocol.Running ? Color.Green : (protocol.ScheduledStartCallback != null ? Color.Olive : Color.Red);
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                SensusException.Report("Invalid call to " + GetType().FullName + ".ConvertBack.");
                return null;
            }
        }

        public static void ExecuteActionUponProtocolAuthentication(Protocol protocol, Action successAction, Action failAction = null)
        {
            if (protocol.LockPasswordHash == "")
                successAction();
            else
            {
                SensusServiceHelper.Get().PromptForInputAsync(
                    "Authenticate \"" + protocol.Name + "\"",
                    new SingleLineTextInput("Protocol Password:", Keyboard.Text, true),
                    null,
                    true,
                    null,
                    null,
                    null,
                    null,
                    false,
                    input =>
                    {
                        if (input == null)
                        {
                            if (failAction != null)
                                failAction();
                        }
                        else
                        {
                            string password = input.Value as string;

                            if (password != null && SensusServiceHelper.Get().GetHash(password) == protocol.LockPasswordHash)
                                successAction();
                            else
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("The password you entered was not correct.");

                                if (failAction != null)
                                    failAction();
                            }
                        }
                    });
            }
        }

        private ListView _protocolsList;

        private void Refresh()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                Bind();
            });
        }

        public ProtocolsPage()
        {
            Title = "Your Studies";

            _protocolsList = new ListView();
            _protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", converter: new ProtocolNameValueConverter()));
            _protocolsList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(".", converter: new ProtocolColorValueConverter()));
            _protocolsList.ItemTapped += async (o, e) =>
            {
                if (_protocolsList.SelectedItem == null)
                {
                    return;
                }

                Protocol selectedProtocol = _protocolsList.SelectedItem as Protocol;

                List<string> actions = new List<string>();

                actions.Add(selectedProtocol.Running ? "Stop" : "Start");

                if(!string.IsNullOrWhiteSpace(selectedProtocol.ContactEmail))
                {
                    actions.Add("Email Study Manager for Help");
                }

                actions.Add("View Data");

                if (selectedProtocol.Running)
                {
                    actions.Add("Display Participation");
                }

                if (selectedProtocol.RemoteDataStore?.CanRetrieveCommittedData ?? false)
                {
                    actions.Add("Scan Participation Barcode");
                }

                actions.AddRange(new string[] { "Edit", "Copy", "Share" });

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

                if (selectedProtocol.Running)
                {
                    actions.Add("Status");
                }

                if (!selectedProtocol.Running && selectedProtocol.ScheduledStartCallback != null)
                {
                    actions.Remove("Start");
                    actions.Insert(0, "Cancel Scheduled Start");
                }

                actions.Add("Delete");

                string selectedAction = await DisplayActionSheet(selectedProtocol.Name, "Cancel", null, actions.ToArray());

                // must reset the protocol selection manually
                Device.BeginInvokeOnMainThread(() =>
                {
                    _protocolsList.SelectedItem = null;
                });

                if (selectedAction == "Start")
                {
                    selectedProtocol.StartWithUserAgreementAsync(null, () =>
                    {
                        // rebind to pick up color and running status changes
                        Refresh();
                    });
                }
                else if (selectedAction == "Cancel Scheduled Start")
                {
                    if (await DisplayAlert("Confirm Cancel", "Are you sure you want to cancel " + selectedProtocol.Name + "?", "Yes", "No"))
                    {
                        selectedProtocol.CancelScheduledStart();

                        // rebind to pick up color and running status changes
                        Refresh();
                    }
                }
                else if (selectedAction == "Stop")
                {
                    if (await DisplayAlert("Confirm Stop", "Are you sure you want to stop " + selectedProtocol.Name + "?", "Yes", "No"))
                    {
                        selectedProtocol.StopAsync(() =>
                        {
                            // rebind to pick up color and running status changes
                            Refresh();
                        });
                    }
                }
                else if (selectedAction == "Email Study Manager for Help")
                {

                    SensusServiceHelper.Get().SendEmailAsync(selectedProtocol.ContactEmail, "Help with Sensus study:  " + selectedProtocol.Name,
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
                else if (selectedAction == "Display Participation")
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                    // pop up wait screen while we submit the participation reward datum
                    SensusServiceHelper.Get().PromptForInputsAsync(
                        null,
                        new[] { new InputGroup { Name = "Please Wait", Inputs = { new LabelOnlyInput("Submitting participation information.", false) } } },
                        cancellationTokenSource.Token,
                        false,
                        "Cancel",
                        null,
                        null,
                        null,
                        false,
                        async () =>
                        {
                            // add participation reward datum to remote data store and commit immediately
                            ParticipationRewardDatum participationRewardDatum = new ParticipationRewardDatum(DateTimeOffset.UtcNow, selectedProtocol.Participation);

                            bool commitFailed;

                            try
                            {
                                commitFailed = !await selectedProtocol.RemoteDataStore.CommitAsync(participationRewardDatum, cancellationTokenSource.Token);
                            }
                            catch (Exception)
                            {
                                commitFailed = true;
                            }

                            if (commitFailed)
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("Failed to submit participation information to remote server. You will not be able to verify your participation at this time.");
                            }

                            // cancel the token to close the input above, but only if the token hasn't already been canceled.
                            if (!cancellationTokenSource.IsCancellationRequested)
                            {
                                cancellationTokenSource.Cancel();
                            }

                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                // only show the QR code for the reward datum if the datum was committed to the remote data store and if the data store can retrieve it.
                                await Navigation.PushAsync(new ParticipationReportPage(selectedProtocol, participationRewardDatum, !commitFailed && (selectedProtocol.RemoteDataStore?.CanRetrieveCommittedData ?? false)));
                            });
                        },
                        inputs =>
                        {
                            // if the prompt was closed by the user instead of the cancellation token, cancel the token in order
                            // to cancel the remote data store commit. if the prompt was closed by the termination of the remote
                            // data store commit (i.e., by the canceled token), then don't cancel the token again.
                            if (!cancellationTokenSource.IsCancellationRequested)
                            {
                                cancellationTokenSource.Cancel();
                            }
                        });
                }
                else if (selectedAction == "Scan Participation Barcode")
                {
                    try
                    {

#if __ANDROID__ || __IOS__

                        ZXingScannerPage barcodeScannerPage = new ZXingScannerPage(new MobileBarcodeScanningOptions
                        {
                            PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE }.ToList()
                        });

                        barcodeScannerPage.OnScanResult += (barcodeResult) =>
                        {
                            barcodeScannerPage.IsScanning = false;

                            if (barcodeResult == null)
                            {
                                return;
                            }

                            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                            {
                                Navigation.PopAsync();

                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                                // pop up wait screen while we get the participation reward datum
                                SensusServiceHelper.Get().PromptForInputsAsync(
                                    null,
                                    new[] { new InputGroup { Name = "Please Wait", Inputs = { new LabelOnlyInput("Retrieving participation information.", false) } } },
                                    cancellationTokenSource.Token,
                                    false,
                                    "Cancel",
                                    null,
                                    null,
                                    null,
                                    false,
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
                                                await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
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
                                            {
                                                cancellationTokenSource.Cancel();
                                            }
                                        }
                                    },
                                    inputs =>
                                    {
                                        // if the prompt was closed by the user instead of the cancellation token, cancel the token in order
                                        // to cancel the datum retrieval. if the prompt was closed by the termination of the remote
                                        // data store get (i.e., by the canceled token), then don't cancel the token again.
                                        if (!cancellationTokenSource.IsCancellationRequested)
                                        {
                                            cancellationTokenSource.Cancel();
                                        }
                                    });
                            });
                        };

                        await Navigation.PushAsync(barcodeScannerPage);
#endif
                    }
                    catch (Exception ex)
                    {
                        string message = "Failed to scan barcode:  " + ex.Message;
                        SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                        SensusServiceHelper.Get().FlashNotificationAsync(message);
                    }
                }
                else if (selectedAction == "Edit")
                {
                    ExecuteActionUponProtocolAuthentication(selectedProtocol, () =>
                    {
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            ProtocolPage protocolPage = new ProtocolPage(selectedProtocol);
                            protocolPage.Disappearing += (oo, ee) => Bind();  // rebind to pick up name changes
                            await Navigation.PushAsync(protocolPage);
                        });
                    });
                }
                else if (selectedAction == "Copy")
                {
                    // reset the protocol id, as we're creating a new study
                    selectedProtocol.CopyAsync(true, true);
                }
                else if (selectedAction == "Share")
                {
                    Action ShareSelectedProtocol = new Action(() =>
                    {
                        // make a deep copy of the selected protocol so we can reset it for sharing. don't reset the id of the protocol to keep
                        // it in the same study. also do not register the copy since we're just going to send it off.
                        selectedProtocol.CopyAsync(false, false, selectedProtocolCopy =>
                        {
                            // write protocol to file and share
                            string sharePath = SensusServiceHelper.Get().GetSharePath(".json");
                            selectedProtocolCopy.Save(sharePath);
                            SensusServiceHelper.Get().ShareFileAsync(sharePath, "Sensus Protocol:  " + selectedProtocolCopy.Name, "application/json");
                        });
                    });

                    if (selectedProtocol.Shareable)
                    {
                        ShareSelectedProtocol();
                    }
                    else
                    {
                        ExecuteActionUponProtocolAuthentication(selectedProtocol, ShareSelectedProtocol);
                    }
                }
                else if (selectedAction == "Group")
                {
                    SensusServiceHelper.Get().PromptForInputAsync("Group",
                        new ItemPickerPageInput("Select Protocols", groupableProtocols.Cast<object>().ToList(), "Name")
                        {
                            Multiselect = true
                        },
                        null, true, "Group", null, null, null, false,
                        input =>
                        {
                            if (input == null)
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("No protocols grouped.");
                                return;
                            }

                            ItemPickerPageInput itemPickerPageInput = input as ItemPickerPageInput;

                            List<Protocol> selectedProtocols = (itemPickerPageInput.Value as List<object>).Cast<Protocol>().ToList();

                            if (selectedProtocols.Count == 0)
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("No protocols grouped.");
                            }
                            else
                            {
                                selectedProtocol.GroupedProtocols.AddRange(selectedProtocols);
                                SensusServiceHelper.Get().FlashNotificationAsync("Grouped \"" + selectedProtocol.Name + "\" with " + selectedProtocols.Count + " other protocol" + (selectedProtocols.Count == 1 ? "" : "s") + ".");
                            }
                        });
                }
                else if (selectedAction == "Ungroup")
                {
                    if (await DisplayAlert("Ungroup " + selectedProtocol.Name + "?", "This protocol is currently grouped with the following other protocols:" + Environment.NewLine + Environment.NewLine + string.Concat(selectedProtocol.GroupedProtocols.Select(protocol => protocol.Name + Environment.NewLine)), "Ungroup", "Cancel"))
                    {
                        selectedProtocol.GroupedProtocols.Clear();
                    }
                }
                else if (selectedAction == "Status")
                {
                    if (SensusServiceHelper.Get().ProtocolShouldBeRunning(selectedProtocol))
                    {
                        await selectedProtocol.TestHealthAsync(true);

                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            if (selectedProtocol.MostRecentReport == null)
                            {
                                await DisplayAlert("No Report", "Status check failed.", "OK");
                            }
                            else
                            {
                                await Navigation.PushAsync(new ViewTextLinesPage("Protocol Status", selectedProtocol.MostRecentReport.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(), null, null));
                            }
                        });
                    }
                    else
                        await DisplayAlert("Protocol Not Running", "Cannot check status of protocol when protocol is not running.", "OK");
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedProtocol.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        selectedProtocol.DeleteAsync();
                    }
                }
            };

            Content = _protocolsList;

            ToolbarItems.Add(new ToolbarItem(null, "gear_wrench.png", async () =>
            {
                double shareDirectoryMB = SensusServiceHelper.GetDirectorySizeMB(SensusServiceHelper.SHARE_DIRECTORY);
                string clearShareDirectoryAction = "Clear Share Directory (" + Math.Round(shareDirectoryMB, 1) + " MB)";

                List<string> buttons = new string[] { "Create Protocol", "Download Protocol", "View Device ID", "View Log", "View Points of Interest", clearShareDirectoryAction }.ToList();

                // stopping only makes sense on android, where we use a background service. on ios, there is no concept
                // of stopping the app other than the user or system terminating the app.
#if __ANDROID__
                buttons.Add("Stop Sensus");
#endif

                buttons.Add("About Sensus");

                string action = await DisplayActionSheet("Other Actions", "Back", null, buttons.ToArray());

                if (action == "Create Protocol")
                {
                    Protocol.Create("New Protocol");
                }
                else if (action == "Download Protocol")
                {
                    SensusServiceHelper.Get().PromptForInputAsync(
                        "Download Protocol",
                        new SingleLineTextInput("Protocol URL:", Keyboard.Url),
                        null, true, null, null, null, null, false, input =>
                    {
                        // input might be null (user cancelled), or the value might be null (blank input submitted)
                        if (!string.IsNullOrEmpty(input?.Value?.ToString()))
                        {
                            Protocol.DeserializeAsync(new Uri(input.Value.ToString()), Protocol.DisplayAndStartAsync);
                        }
                    });
                }
                else if (action == "View Device ID")
                {
                    await DisplayAlert("Device ID", SensusServiceHelper.Get().DeviceId, "Close");
                }
                else if (action == "View Log")
                {
                    await Navigation.PushAsync(new ViewTextLinesPage("Log", SensusServiceHelper.Get().Logger.Read(200, true),
                        () =>
                        {
                            string sharePath = null;
                            try
                            {
                                sharePath = SensusServiceHelper.Get().GetSharePath(".txt");
                                SensusServiceHelper.Get().Logger.CopyTo(sharePath);
                            }
                            catch (Exception)
                            {
                                sharePath = null;
                            }

                            if (sharePath != null)
                            {
                                SensusServiceHelper.Get().ShareFileAsync(sharePath, "Log:  " + Path.GetFileName(sharePath), "text/plain");
                            }
                        },
                        () => SensusServiceHelper.Get().Logger.Clear()));
                }
                else if (action == "View Points of Interest")
                {
                    await Navigation.PushAsync(new PointsOfInterestPage(SensusServiceHelper.Get().PointsOfInterest));
                }
                else if (action == clearShareDirectoryAction)
                {
                    foreach (string sharePath in Directory.GetFiles(SensusServiceHelper.SHARE_DIRECTORY))
                    {
                        try
                        {
                            File.Delete(sharePath);
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = "Failed to delete shared file \"" + Path.GetFileName(sharePath) + "\":  " + ex.Message;
                            SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);
                            SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                        }
                    }
                }
#if __ANDROID__
                else if (action == "Stop Sensus" && await DisplayAlert("Confirm", "Are you sure you want to stop Sensus? This will end your participation in all studies.", "Stop Sensus", "Go Back"))
                {
                    SensusServiceHelper.Get().StopProtocols();
                    (SensusServiceHelper.Get() as Android.IAndroidSensusServiceHelper)?.StopAndroidSensusService();
                }
#endif
                else if (action == "About Sensus")
                {
                    await DisplayAlert("About Sensus", "Version:  " + SensusServiceHelper.Get().Version, "OK");
                }
            }));

            Bind();

            System.Timers.Timer refreshTimer = new System.Timers.Timer(1000);

            refreshTimer.Elapsed += (sender, e) =>
            {
                Refresh();
            };

            Appearing += (sender, e) =>
            {
                refreshTimer.Start();
            };

            Disappearing += (sender, e) =>
            {
                refreshTimer.Stop();
            };
        }

        public void Bind()
        {
            _protocolsList.ItemsSource = null;

            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            // make sure we have a service helper -- it might get disconnected before we get the OnDisappearing event that calls Bind
            if (serviceHelper != null)
            {
                _protocolsList.ItemsSource = serviceHelper.RegisteredProtocols;
            }
        }
    }
}