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
using System;
using System.IO;
using System.Linq;
using Xamarin.Forms;
using SensusUI.Inputs;
using System.Collections.Generic;
using SensusService.Probes.User;
using SensusService.Probes;

namespace SensusUI
{
    /// <summary>
    /// Displays all protocols.
    /// </summary>
    public class ProtocolsPage : ContentPage
    {
        public static void ExecuteActionUponProtocolAuthentication(Protocol protocol, Action successAction, Action failAction = null)
        {
            if (protocol.LockPasswordHash == "")
                successAction();
            else
            {
                UiBoundSensusServiceHelper.Get(true).PromptForInputAsync(

                    "Authenticate \"" + protocol.Name + "\"", 

                    new TextInput("Protocol Password:"),

                    null,

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

                            if (password != null && UiBoundSensusServiceHelper.Get(true).GetHash(password) == protocol.LockPasswordHash)
                                successAction();
                            else
                            {
                                UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("The password you entered was not correct.");

                                if (failAction != null)
                                    failAction();
                            }
                        }
                    });
            }
        }

        private ListView _protocolsList;

        public ProtocolsPage()
        {
            Title = "Protocols";

            _protocolsList = new ListView();
            _protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            _protocolsList.ItemTapped += async (o, e) =>
            {                                    
                if (_protocolsList.SelectedItem == null)
                    return;

                Protocol selectedProtocol = _protocolsList.SelectedItem as Protocol;

                string selectedAction = await DisplayActionSheet(selectedProtocol.Name, "Cancel", null, selectedProtocol.Running ? "Stop" : "Start", "Edit", "Status", "Share", "Group", "Delete");

                if (selectedAction == "Start")
                    selectedProtocol.StartWithUserAgreementAsync(null);
                else if (selectedAction == "Stop")
                {
                    if (await DisplayAlert("Confirm Stop", "Are you sure you want to stop " + selectedProtocol.Name + "?", "Yes", "No"))
                        selectedProtocol.Running = false;
                }
                else if (selectedAction == "Edit")
                {
                    ExecuteActionUponProtocolAuthentication(selectedProtocol, () => Device.BeginInvokeOnMainThread(async () =>
                            {
                                ProtocolPage protocolPage = new ProtocolPage(selectedProtocol);
                                protocolPage.Disappearing += (oo, ee) => Bind();  // rebind to pick up name changes
                                await Navigation.PushAsync(protocolPage);
                                _protocolsList.SelectedItem = null;
                            }));
                }
                else if (selectedAction == "Status")
                {
                    if (UiBoundSensusServiceHelper.Get(true).ProtocolShouldBeRunning(selectedProtocol))
                    {
                        selectedProtocol.TestHealthAsync(true, () =>
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                    {
                                        if (selectedProtocol.MostRecentReport == null)
                                            await DisplayAlert("No Report", "Status check failed.", "OK");
                                        else
                                            await Navigation.PushAsync(new ViewTextLinesPage("Protocol Report", selectedProtocol.MostRecentReport.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(), null));
                                    });
                            });
                    }
                    else
                        await DisplayAlert("Protocol Not Running", "Cannot check status of protocol when protocol is not running.", "OK");
                }
                else if (selectedAction == "Share")
                {
                    Action ShareSelectedProtocol = new Action(() =>
                        {
                            // make a deep copy of the selected protocol so we can reset it for sharing
                            selectedProtocol.CopyAsync(selectedProtocolCopy =>
                                {
                                    selectedProtocolCopy.ResetForSharing();

                                    // write protocol to file and share
                                    string sharePath = UiBoundSensusServiceHelper.Get(true).GetSharePath(".sensus");
                                    selectedProtocolCopy.Save(sharePath);
                                    UiBoundSensusServiceHelper.Get(true).ShareFileAsync(sharePath, "Sensus Protocol:  " + selectedProtocolCopy.Name);
                                });
                        });

                    if (selectedProtocol.Shareable)
                        ShareSelectedProtocol();
                    else
                        ExecuteActionUponProtocolAuthentication(selectedProtocol, ShareSelectedProtocol);
                }
                else if (selectedAction == "Group")
                {
                    if (selectedProtocol.Groupable)
                    {
                        if (UiBoundSensusServiceHelper.Get(true).RegisteredProtocols.Count(registeredProtocol => registeredProtocol != selectedProtocol && registeredProtocol.Groupable && registeredProtocol.GroupedProtocols.Count == 0) == 0)
                            UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("No protocols available to group with selected protocol.");
                        else
                            await Navigation.PushAsync(new GroupProtocolPage(selectedProtocol));
                    }
                    else
                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Selected protocol is not groupable.");
                }
                else if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedProtocol.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        selectedProtocol.StopAsync(() =>
                            {
                                UiBoundSensusServiceHelper.Get(true).UnregisterProtocol(selectedProtocol);

                                try
                                {
                                    Directory.Delete(selectedProtocol.StorageDirectory, true);
                                }
                                catch (Exception ex)
                                {
                                    UiBoundSensusServiceHelper.Get(true).Logger.Log("Failed to delete protocol storage directory \"" + selectedProtocol.StorageDirectory + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                                }

                                Device.BeginInvokeOnMainThread(() =>
                                    {
                                        _protocolsList.SelectedItem = null;  // must reset this manually, since it isn't reset automatically
                                    });
                            });
                    }
                }
            };
            
            Bind();

            Content = _protocolsList;
            
            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                    {
                        Protocol.CreateAsync("New Protocol", null);
                    }));
        }

        public void Bind()
        {
            _protocolsList.ItemsSource = null;

            // don't wait for service helper -- it might get disconnected before we get the OnDisappearing event that calls Bind
            SensusServiceHelper serviceHelper = UiBoundSensusServiceHelper.Get(false);
            if (serviceHelper != null)
                _protocolsList.ItemsSource = serviceHelper.RegisteredProtocols;
        }
    }
}