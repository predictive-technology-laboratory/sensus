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
    public class ProtocolsPage : ContentPage
    {
        private ListView _protocolsList;

        public ProtocolsPage()
        {
            Title = "Protocols";

            _protocolsList = new ListView();
            _protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");

            Bind();

            Content = _protocolsList;

            #region toolbar
            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                {
                    UiBoundSensusServiceHelper.Get(true).RegisterProtocol(new Protocol("New Protocol"));

                    _protocolsList.ItemsSource = null;
                    _protocolsList.ItemsSource = UiBoundSensusServiceHelper.Get(true).RegisteredProtocols;
                }));

            ToolbarItems.Add(new ToolbarItem(null, "minus.png", () =>
                {
                    if (_protocolsList.SelectedItem != null)
                    {
                        Protocol protocolToDelete = _protocolsList.SelectedItem as Protocol;

                        Action deleteProtocol = new Action(async () =>
                            {
                                if (await DisplayAlert("Delete " + protocolToDelete.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                                {
                                    protocolToDelete.StopAsync(() =>
                                        {
                                            UiBoundSensusServiceHelper.Get(true).UnregisterProtocol(protocolToDelete);

                                            try { Directory.Delete(protocolToDelete.StorageDirectory, true); }
                                            catch (Exception ex) { UiBoundSensusServiceHelper.Get(true).Logger.Log("Failed to delete protocol storage directory \"" + protocolToDelete.StorageDirectory + "\":  " + ex.Message, LoggingLevel.Normal, GetType()); }

                                            Device.BeginInvokeOnMainThread(() =>
                                                {
                                                    _protocolsList.ItemsSource = _protocolsList.ItemsSource.Cast<Protocol>().Where(p => p != protocolToDelete);
                                                    _protocolsList.SelectedItem = null;
                                                });
                                        });
                                }
                            });

                        if(protocolToDelete.LockPasswordHash == "")
                            deleteProtocol();
                        else
                            UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Enter protocol password to delete:", false, password => 
                                {
                                    if(password == null)
                                        return;

                                    if(UiBoundSensusServiceHelper.Get(true).GetMd5Hash(password) == protocolToDelete.LockPasswordHash)
                                        Device.BeginInvokeOnMainThread(deleteProtocol);
                                    else
                                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Incorrect password"); 
                                });
                    }
                }));
            
            ToolbarItems.Add(new ToolbarItem(null, "pencil.png", () =>
                {
                    if (_protocolsList.SelectedItem != null)
                    {
                        Protocol protocol = _protocolsList.SelectedItem as Protocol;

                        Action openProtocol = new Action(async() =>
                            {
                                ProtocolPage protocolPage = new ProtocolPage(protocol);
                                protocolPage.Disappearing += (o, e) => Bind();
                                await Navigation.PushAsync(protocolPage);
                                _protocolsList.SelectedItem = null;
                            });
                        
                        if(protocol.LockPasswordHash == "")
                            openProtocol();
                        else
                            UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Enter protocol password to open:", false, password => 
                                {
                                    if(password == null)
                                        return;
                                    
                                    if(UiBoundSensusServiceHelper.Get(true).GetMd5Hash(password) == protocol.LockPasswordHash)
                                        Device.BeginInvokeOnMainThread(openProtocol);
                                    else
                                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Incorrect password");
                                });
                    }
                }));

            ToolbarItems.Add(new ToolbarItem(null, "share.png", () =>
                {
                    if (_protocolsList.SelectedItem != null)
                    {
                        Protocol protocol = _protocolsList.SelectedItem as Protocol;

                        Action shareProtocol = new Action(() =>
                            {
                                string path = null;
                                try
                                {
                                    path = UiBoundSensusServiceHelper.Get(true).GetSharePath(".sensus");
                                    protocol.Save(path);
                                }
                                catch (Exception ex)
                                {
                                    UiBoundSensusServiceHelper.Get(true).Logger.Log("Failed to save protocol to file for sharing:  " + ex.Message, LoggingLevel.Normal, GetType());
                                    path = null;
                                }

                                if (path != null)
                                    UiBoundSensusServiceHelper.Get(true).ShareFileAsync(path, "Sensus Protocol:  " + protocol.Name);
                            });
                        
                        if(protocol.Shareable || protocol.LockPasswordHash == "")
                            shareProtocol();
                        else
                            UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Enter protocol password to share:", false, password => 
                                {
                                    if(password == null)
                                        return;

                                    if(UiBoundSensusServiceHelper.Get(true).GetMd5Hash(password) == protocol.LockPasswordHash)
                                        Device.BeginInvokeOnMainThread(shareProtocol);
                                    else
                                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Incorrect password");
                                });
                    }
                }));
            #endregion
        }

        public void Bind()
        {
            _protocolsList.ItemsSource = null;

            // don't wait for service helper -- it might be disconnected before we get the OnDisappearing event that calls Bind
            SensusServiceHelper serviceHelper = UiBoundSensusServiceHelper.Get(false);
            if (serviceHelper != null)
                _protocolsList.ItemsSource = serviceHelper.RegisteredProtocols;
        }
    }
}
