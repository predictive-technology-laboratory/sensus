//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
                CrossClipboard.Current.SetText(protocol.Id);
                await SensusServiceHelper.Get().FlashNotificationAsync("Copied study identifier to clipboard.");
            };

            views.Add(copyIdButton);

            Button setIdButton = new Button
            {
                Text = "Set Study Identifier",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            setIdButton.Clicked += async (o, e) =>
            {
                Input input = await SensusServiceHelper.Get().PromptForInputAsync("Set Study Identifier", new SingleLineTextInput("Identifier:", "id", Keyboard.Text)
                {
                    Required = true

                }, CancellationToken.None, true, "Set", null, null, "Are you sure you wish to set the study identifier? This should not be necessary under normal circumstances. Proceed only if you understand the implications.", false);

                if (string.IsNullOrWhiteSpace(input?.Value?.ToString()))
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Identifier not set.");
                    return;
                }

                string newId = input.Value.ToString().Trim();

                if (protocol.Id == newId)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Identifier unchanged.");
                }
                else if (SensusServiceHelper.Get().RegisteredProtocols.Any(p => p.Id == newId))
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("A study with the same identifier already exists.");
                }
                else
                {
                    protocol.Id = newId;
                    await SensusServiceHelper.Get().FlashNotificationAsync("Identifier set.");
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
                        byte[] encryptedBytes = encryptor.Encrypt(protocolJSON);
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
                    editLocalDataStoreButton.IsEnabled = createLocalDataStoreButton.IsEnabled = editRemoteDataStoreButton.IsEnabled = createRemoteDataStoreButton.IsEnabled = state == ProtocolState.Stopped;
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
    }
}
