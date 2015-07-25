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

namespace SensusUI
{
    /// <summary>
    /// Displays all protocols.
    /// </summary>
    public class ProtocolsPage : ContentPage
    {
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

                string selectedAction = await DisplayActionSheet(selectedProtocol.Name, "Cancel", null, selectedProtocol.Running ? "Stop" : "Start", "Edit", "Status", "Share", "Delete");

                if (selectedAction == "Start")
                    selectedProtocol.StartWithUserAgreement(null);
                else if (selectedAction == "Stop")
                    selectedProtocol.Running = false;
                else if (selectedAction == "Edit")
                {
                    Action editAction = new Action(async() =>
                        {
                            ProtocolPage protocolPage = new ProtocolPage(selectedProtocol);
                            protocolPage.Disappearing += (oo, ee) => Bind();  // rebind to pick up name changes
                            await Navigation.PushAsync(protocolPage);
                            _protocolsList.SelectedItem = null;
                        });

                    ExecuteActionUponProtocolAuthentication(selectedProtocol, editAction);
                }
                else if (selectedAction == "Status")
                {
                    if (UiBoundSensusServiceHelper.Get(true).ProtocolShouldBeRunning(selectedProtocol))
                    {
                        selectedProtocol.TestHealthAsync(() =>
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
                    Action shareAction = new Action(() =>
                        {
                            string path = null;
                            try
                            {
                                path = UiBoundSensusServiceHelper.Get(true).GetSharePath(".sensus");
                                selectedProtocol.Save(path);
                            }
                            catch (Exception ex)
                            {
                                UiBoundSensusServiceHelper.Get(true).Logger.Log("Failed to save protocol to file for sharing:  " + ex.Message, LoggingLevel.Normal, GetType());
                                path = null;
                            }

                            if (path != null)
                                UiBoundSensusServiceHelper.Get(true).ShareFileAsync(path, "Sensus Protocol:  " + selectedProtocol.Name);
                        });

                    // don't authenticate if the protocol was declared shareable -- participants might require the ability to share without the password.
                    if (selectedProtocol.Shareable)
                        shareAction();
                    // if the protocol isn't declared shareable, require authentication, since sharing is equivalent to editing the protocol.
                    else
                        ExecuteActionUponProtocolAuthentication(selectedProtocol, shareAction);
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
                        Protocol.CreateAsync("New Protocol", protocol =>
                            {
                                UiBoundSensusServiceHelper.Get(true).RegisterProtocol(protocol);
                            });
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

        private void ExecuteActionUponProtocolAuthentication(Protocol protocol, Action action)
        {
            if (protocol.LockPasswordHash == "")
                action();
            else
                UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Enter protocol password:", false, password =>
                    {
                        if (password == null)
                            return;

                        if (UiBoundSensusServiceHelper.Get(true).GetHash(password) == protocol.LockPasswordHash)
                            Device.BeginInvokeOnMainThread(action);
                        else
                            UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("The password you entered was not correct."); 
                    });
        }
    }
}