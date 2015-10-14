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
using System.Collections.Generic;
using SensusService;
using SensusService.Probes;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using SensusService.Exceptions;

namespace SensusUI
{
    public class ShareProtocolsPage : ContentPage
    {
        private class ProtocolTextColorValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object selectedProtocols, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return Color.Gray;
                
                Protocol protocol = value as Protocol;
                List<Protocol> protocols = selectedProtocols as List<Protocol>;

                return protocols.Contains(protocol) ? Color.Accent : Color.Gray;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        public ShareProtocolsPage(List<Protocol> protocols)
        {   
            Title = "Select Protocols To Share";

            List<Protocol> selectedProtocols = new List<Protocol>();

            ListView protocolsList = new ListView();
            protocolsList.ItemTemplate = new DataTemplate(typeof(TextCell));
            protocolsList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");
            protocolsList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(".", converter: new ProtocolTextColorValueConverter(), converterParameter: selectedProtocols));
            protocolsList.ItemsSource = protocols;

            protocolsList.ItemTapped += (o, e) =>
            {
                Protocol selectedProtocol = e.Item as Protocol;

                if (selectedProtocols.Contains(selectedProtocol))
                    selectedProtocols.Remove(selectedProtocol);
                else
                    selectedProtocols.Add(selectedProtocol);

                protocolsList.ItemsSource = null;
                protocolsList.ItemsSource = protocols;
            };

            ToolbarItems.Add(new ToolbarItem("OK", null, async () =>
                    {
                        // start new thread because we block below while waiting for protocol authentication and copying, both of which require the UI thread.
                        new Thread(() =>
                            {
                                try
                                {
                                    // copy all selected protocols
                                    List<Protocol> selectedProtocolCopies = new List<Protocol>();
                                    foreach (Protocol selectedProtocol in selectedProtocols)
                                    {
                                        ManualResetEvent copyWait = new ManualResetEvent(false);

                                        Action CopySelectedProtocolAsync = new Action(() =>
                                            {
                                                // make a deep copy of the selected protocols so we can reset it or sharing
                                                selectedProtocol.CopyAsync(selectedProtocolCopy =>
                                                    {
                                                        selectedProtocolCopy.ResetForSharing();
                                                        selectedProtocolCopies.Add(selectedProtocolCopy);
                                                        copyWait.Set();
                                                    });
                                            });
                            
                                        // if the protocol is marked as shareable, share directly; otherwise, authenticate first and then share.
                                        if (selectedProtocol.Shareable)
                                            CopySelectedProtocolAsync();
                                        else
                                            ProtocolsPage.ExecuteActionUponProtocolAuthentication(selectedProtocol, CopySelectedProtocolAsync, () => copyWait.Set());

                                        // wait for asynchronous copying actions
                                        copyWait.WaitOne();
                                    }

                                    if (selectedProtocolCopies.Count == 0)
                                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("No protocols shared.");
                                    else
                                    {
                                        string sharePath = UiBoundSensusServiceHelper.Get(true).GetSharePath(".sensus");

                                        using (FileStream shareFile = new FileStream(sharePath, FileMode.Create, FileAccess.Write))
                                        {
                                            byte[] encryptedBytes = SensusServiceHelper.Encrypt(JsonConvert.SerializeObject(selectedProtocolCopies, SensusServiceHelper.JSON_SERIALIZER_SETTINGS));
                                            shareFile.Write(encryptedBytes, 0, encryptedBytes.Length);
                                            shareFile.Close();
                                        }

                                        // make sure to call share file strictly after the flash notification, since both are asynchronous and the latter can interrupt the former on android
                                        UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Sharing " + selectedProtocolCopies.Count + " protocol" + (selectedProtocolCopies.Count == 1 ? "" : "s") + ".", () =>
                                            {
                                                UiBoundSensusServiceHelper.Get(true).ShareFileAsync(sharePath, "Sensus Protocol" + (selectedProtocolCopies.Count == 1 ? ":  " + selectedProtocolCopies[0].Name : "s"));
                                            });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = "Failed to share protocol:  " + ex.Message;
                                    UiBoundSensusServiceHelper.Get(true).Logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                                    UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync(errorMessage);
                                }

                            }).Start();

                        await Navigation.PopAsync();

                    }));

            Content = protocolsList;
        }
    }
}