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

using Sensus.DataStores;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.UI.UiProperties;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Sensus.Context;
using Sensus.UI.Inputs;
using Xamarin.Forms;
using System.Threading;
using Plugin.Clipboard;
using Newtonsoft.Json;
using Sensus.Encryption;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Sensus.UI
{
    /// <summary>
    /// Displays a single protocol.
    /// </summary>
    public class ProtocolPage : ContentPage
    {
        private Protocol _protocol;
        private EventHandler<ProtocolState> _protocolStateChangedAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolPage"/> class.
        /// </summary>
        /// <param name="protocol">Protocol to display.</param>
        public ProtocolPage(Protocol protocol)
        {
            _protocol = protocol;

            Title = "Protocol";

            List<View> views = new List<View>();

            views.AddRange(UiProperty.GetPropertyStacks(_protocol));

            #region copy/set id
            Button copyIdButton = new Button
            {
                Text = "Copy Study Identifier",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            copyIdButton.Clicked += async (o, e) =>
            {
                CrossClipboard.Current.SetText(_protocol.Id);
                await SensusServiceHelper.Get().FlashNotificationAsync("Copied study identifier to clipboard.");
            };

            views.Add(copyIdButton);

            Button setIdButton = new Button
            {
                Text = "Set Study Identifier",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                IsEnabled = _protocol.State == ProtocolState.Stopped
            };

            setIdButton.Clicked += async (o, e) =>
            {
                if (await DisplayAlert("Confirm", "Setting the study identifier should not be necessary. Proceed only if you understand the consequences. Do you wish to proceed?", "Yes", "No"))
                {
                    string newId = null;

                    if (await DisplayAlert("Random Identifier", "Do you wish to use a random identifier?", "Yes", "No"))
                    {
                        newId = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        Input input = await SensusServiceHelper.Get().PromptForInputAsync("Study Identifier", new SingleLineTextInput("Identifier:", "id", Keyboard.Text)
                        {
                            Required = true

                        }, CancellationToken.None, true, "Set", null, null, null, false);

                        newId = input?.Value?.ToString().Trim();
                    }

                    if (string.IsNullOrWhiteSpace(newId))
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("No identifier supplied. Identifier not set.");
                    }
                    else
                    {
                        if (_protocol.Id == newId)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Identifier unchanged.");
                        }
                        else if (SensusServiceHelper.Get().RegisteredProtocols.Any(p => p.Id == newId))
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("A study with the same identifier already exists. Identifier not set.");
                        }
                        else
                        {
                            _protocol.Id = newId;
                            await SensusServiceHelper.Get().FlashNotificationAsync("Identifier set.");
                        }
                    }
                }
            };

            views.Add(setIdButton);
            #endregion  

            #region data stores
            string localDataStoreSize = _protocol.LocalDataStore?.SizeDescription;

            Button editLocalDataStoreButton = new Button
            {
                Text = "Local Data Store" + (localDataStoreSize == null ? "" : " (" + localDataStoreSize + ")"),
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                IsEnabled = _protocol.State == ProtocolState.Stopped
            };

            editLocalDataStoreButton.Clicked += async (o, e) =>
            {
                if (_protocol.LocalDataStore != null)
                {
                    DataStore copy = _protocol.LocalDataStore.Copy();

                    if (copy == null)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Failed to edit data store.");
                    }
                    else
                    {
                        await Navigation.PushAsync(new DataStorePage(_protocol, copy, true, false));
                    }
                }
            };

            Button createLocalDataStoreButton = new Button
            {
                Text = "+",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.End,
                IsEnabled = _protocol.State == ProtocolState.Stopped
            };

            createLocalDataStoreButton.Clicked += (o, e) => CreateDataStore(true);

            StackLayout localDataStoreStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { editLocalDataStoreButton, createLocalDataStoreButton }
            };

            views.Add(localDataStoreStack);

            Button editRemoteDataStoreButton = new Button
            {
                Text = "Remote Data Store",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                IsEnabled = _protocol.State == ProtocolState.Stopped
            };

            editRemoteDataStoreButton.Clicked += async (o, e) =>
            {
                if (_protocol.RemoteDataStore != null)
                {
                    DataStore copy = _protocol.RemoteDataStore.Copy();

                    if (copy == null)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Failed to edit data store.");
                    }
                    else
                    {
                        await Navigation.PushAsync(new DataStorePage(_protocol, copy, false, false));
                    }
                }
            };

            Button createRemoteDataStoreButton = new Button
            {
                Text = "+",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.End,
                IsEnabled = _protocol.State == ProtocolState.Stopped
            };

            createRemoteDataStoreButton.Clicked += (o, e) => CreateDataStore(false);

            StackLayout remoteDataStoreStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { editRemoteDataStoreButton, createRemoteDataStoreButton }
            };

            views.Add(remoteDataStoreStack);
            #endregion

            #region points of interest
            Button pointsOfInterestButton = new Button
            {
                Text = "Points of Interest",
                FontSize = 20
            };

            pointsOfInterestButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new PointsOfInterestPage(_protocol.PointsOfInterest));
            };

            views.Add(pointsOfInterestButton);
            #endregion

            #region view probes
            Button viewProbesButton = new Button
            {
                Text = "Probes",
                FontSize = 20
            };

            viewProbesButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ProbesEditPage(_protocol));
            };

            views.Add(viewProbesButton);
            #endregion

            #region share -- we need this because we need to be able to hide the share button from the protocols while still allowing the protocol to be locked and shared
            Button shareButton = new Button
            {
                Text = "Share Protocol",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            shareButton.Clicked += async (o, e) =>
            {
                await _protocol.ShareAsync();
            };

            views.Add(shareButton);
            #endregion

            #region change encryption key
            Button changeEncryptionKeyButton = new Button
            {
                Text = "Change Encryption Key",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            changeEncryptionKeyButton.Clicked += async (o, e) =>
            {
                Input input = await SensusServiceHelper.Get().PromptForInputAsync("Change Encryption Key", new SingleLineTextInput("Key:", "key", Keyboard.Text)
                {
                    Required = true

                }, CancellationToken.None, true, null, null, null, null, false);

                string key = input?.Value?.ToString().Trim();

                // disallow an empty (i.e., "") key. all sensus apps should have a key.
                if (string.IsNullOrWhiteSpace(key))
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Encryption key unchanged.");
                }
                else
                {
                    try
                    {
                        string protocolJSON = JsonConvert.SerializeObject(_protocol, SensusServiceHelper.JSON_SERIALIZER_SETTINGS);
                        SymmetricEncryption encryptor = new SymmetricEncryption(key);
                        byte[] encryptedBytes = encryptor.Encrypt(protocolJSON, Encoding.Unicode);  // once upon a time, we made the poor decision to encode protocols as unicode (UTF-16). can't switch to UTF-8 now...
                        string sharePath = SensusServiceHelper.Get().GetSharePath(".json");
                        File.WriteAllBytes(sharePath, encryptedBytes);
                        await SensusServiceHelper.Get().ShareFileAsync(sharePath, "Sensus Protocol:  " + _protocol.Name, "application/json");
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while changing encryption key:  " + ex, LoggingLevel.Normal, GetType());
                    }
                }
            };

            views.Add(changeEncryptionKeyButton);
            #endregion

            #region sensing agent
            Button setAgentButton = new Button
            {
                Text = "Set Agent" + (_protocol.Agent == null ? "" : ":  " + _protocol.Agent.Id),
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            setAgentButton.Clicked += async (sender, e) =>
            {
                await SetAgentButton_Clicked(setAgentButton);
            };

            views.Add(setAgentButton);

            Button clearAgentButton = new Button
            {
                Text = "Clear Agent",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            clearAgentButton.Clicked += async (sender, e) =>
            {
                if (_protocol.Agent != null)
                {
                    if (await DisplayAlert("Confirm", "Are you sure you wish to clear the sensing agent?", "Yes", "No"))
                    {
                        _protocol.Agent = null;
                        setAgentButton.Text = "Set Agent";
                    }
                }

                await SensusServiceHelper.Get().FlashNotificationAsync("Sensing agent cleared.");
            };
            #endregion

            #region lock
            Button lockButton = new Button
            {
                Text = _protocol.LockPasswordHash == "" ? "Lock" : "Unlock",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            lockButton.Clicked += async (o, e) =>
            {
                if (lockButton.Text == "Lock")
                {
                    Input input = await SensusServiceHelper.Get().PromptForInputAsync("Lock Protocol", new SingleLineTextInput("Password:", Keyboard.Text, true), null, true, null, null, null, null, false);

                    if (input == null)
                    {
                        return;
                    }

                    string password = input.Value as string;

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Please enter a non-empty password.");
                    }
                    else
                    {
                        _protocol.LockPasswordHash = SensusServiceHelper.Get().GetHash(password);
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() => lockButton.Text = "Unlock");
                    }
                }
                else if (lockButton.Text == "Unlock")
                {
                    _protocol.LockPasswordHash = "";
                    lockButton.Text = "Lock";
                }
            };

            views.Add(lockButton);
            #endregion

            _protocolStateChangedAction = (o, state) =>
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    setIdButton.IsEnabled =                  // don't let the user set the protocol id when the protocol is running. if an auth server is in use, the health test will attempt to replace the protocol because the id will not match that of the credentials
                    editLocalDataStoreButton.IsEnabled =     // don't let the user edit data stores when the protocol is running
                    createLocalDataStoreButton.IsEnabled =   // don't let the user edit data stores when the protocol is running
                    editRemoteDataStoreButton.IsEnabled =    // don't let the user edit data stores when the protocol is running
                    createRemoteDataStoreButton.IsEnabled =  // don't let the user edit data stores when the protocol is running
                    state == ProtocolState.Stopped;
                });
            };

            StackLayout stack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (View view in views)
            {
                stack.Children.Add(view);
            }

            Content = new ScrollView
            {
                Content = stack
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _protocol.StateChanged += _protocolStateChangedAction;
        }

        private async void CreateDataStore(bool local)
        {
            Type dataStoreType = local ? typeof(LocalDataStore) : typeof(RemoteDataStore);

            List<DataStore> dataStores = Assembly.GetExecutingAssembly()
                                                 .GetTypes()
                                                 .Where(t => !t.IsAbstract && t.IsSubclassOf(dataStoreType))
                                                 .Select(Activator.CreateInstance)
                                                 .Cast<DataStore>()
                                                 .OrderBy(d => d.DisplayName)
                                                 .ToList();

            string cancelButtonName = "Cancel";
            string selected = await DisplayActionSheet("Select " + (local ? "Local" : "Remote") + " Data Store", cancelButtonName, null, dataStores.Select((d, i) => (i + 1) + ") " + d.DisplayName).ToArray());
            if (!string.IsNullOrWhiteSpace(selected) && selected != cancelButtonName)
            {
                await Navigation.PushAsync(new DataStorePage(_protocol, dataStores[int.Parse(selected.Substring(0, selected.IndexOf(")"))) - 1], local, true));
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _protocol.StateChanged -= _protocolStateChangedAction;
        }

        private async Task SetAgentButton_Clicked(Button setAgentButton)
        {
            List<Input> agentSelectionInputs = new List<Input>();

            // show any existing agents
            List<SensingAgent> currentAgents = null;

            // android allows us to dynamically load code assemblies, but iOS does not. so, the current approach
            // is to only support dynamic loading on android and force compile-time assembly inclusion on ios.
#if __ANDROID__
            // try to extract agents from a previously loaded assembly
            try
            {
              currentAgents = Protocol.GetAgents(_protocol.AgentAssemblyBytes);
            }
            catch (Exception)
            { }
#elif __IOS__
            currentAgents = Protocol.GetAgents();

            // display warning message, as there is no other option to load agents.
            if (currentAgents.Count == 0)
            {
                await SensusServiceHelper.Get().FlashNotificationAsync("No agents available.");
                return;
            }
#endif

            // let the user pick from currently available agents
            ItemPickerPageInput currentAgentsPicker = null;
            if (currentAgents != null && currentAgents.Count > 0)
            {
                currentAgentsPicker = new ItemPickerPageInput("Available agent" + (currentAgents.Count > 1 ? "s" : "") + ":", currentAgents.Cast<object>().ToList())
                {
                    Required = false
                };

                agentSelectionInputs.Add(currentAgentsPicker);
            }

#if __ANDROID__
            // add option to scan qr code to import a new assembly
            QrCodeInput agentAssemblyUrlQrCodeInput = new QrCodeInput(QrCodePrefix.SENSING_AGENT, "URL:", false, "Agent URL:")
            {
                Required = false
            };

            agentSelectionInputs.Add(agentAssemblyUrlQrCodeInput);
#endif

            List<Input> completedInputs = await SensusServiceHelper.Get().PromptForInputsAsync("Sensing Agent", agentSelectionInputs, null, true, "Set", null, null, null, false);

            if (completedInputs == null)
            {
                return;
            }

            // check for QR code on android. this doesn't exist on ios.
            string agentURL = null;

#if __ANDROID__
            agentURL = agentAssemblyUrlQrCodeInput.Value?.ToString();
#endif

            // if there is no URL, check if the user has selected an agent.
            if (string.IsNullOrWhiteSpace(agentURL))
            {
                if (currentAgentsPicker != null)
                {
                    SensingAgent selectedAgent = (currentAgentsPicker.Value as List<object>).FirstOrDefault() as SensingAgent;

                    // set the selected agent, watching out for a null (clearing) selection that needs to be confirmed
                    if (selectedAgent != null || await DisplayAlert("Confirm", "Are you sure you wish to clear the sensing agent?", "Yes", "No"))
                    {
                        _protocol.Agent = selectedAgent;

                        setAgentButton.Text = "Set Agent" + (_protocol.Agent == null ? "" : ":  " + _protocol.Agent.Id);

                        if (_protocol.Agent == null)
                        {
                            await SensusServiceHelper.Get().FlashNotificationAsync("Sensing agent cleared.");
                        }
                    }
                }
            }
#if __ANDROID__
            else
            {
                // download agent assembly from scanned QR code
                byte[] downloadedBytes = null;
                string downloadErrorMessage = null;
                try
                {
                    // download the assembly and extract agents
                    downloadedBytes = _protocol.AgentAssemblyBytes = await new WebClient().DownloadDataTaskAsync(new Uri(agentURL));
                    List<SensingAgent> qrCodeAgents = Protocol.GetAgents(downloadedBytes);

                    if (qrCodeAgents.Count == 0)
                    {
                        throw new Exception("No agents were present in the specified file.");
                    }
                }
                catch (Exception ex)
                {
                    downloadErrorMessage = ex.Message;
                }

                // if error message is null, then we have 1 or more agents in the downloaded assembly.
                if (downloadErrorMessage == null)
                {
                    // redisplay the current input prompt including the agents we just downloaded
                    _protocol.AgentAssemblyBytes = downloadedBytes;
                    await SetAgentButton_Clicked(setAgentButton);
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log(downloadErrorMessage, LoggingLevel.Normal, GetType());
                    await SensusServiceHelper.Get().FlashNotificationAsync(downloadErrorMessage);
                }
            }
#endif
        }
    }
}