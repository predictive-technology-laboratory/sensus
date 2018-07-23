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

namespace Sensus.UI
{
    /// <summary>
    /// Displays a single protocol.
    /// </summary>
    public class ProtocolPage : ContentPage
    {
        private Protocol _protocol;
        private EventHandler<bool> _protocolRunningChangedAction;

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

            #region data stores
            string localDataStoreSize = _protocol.LocalDataStore?.SizeDescription;

            Button editLocalDataStoreButton = new Button
            {
                Text = "Local Data Store" + (localDataStoreSize == null ? "" : " (" + localDataStoreSize + ")"),
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                IsEnabled = !_protocol.Running
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
                IsEnabled = !_protocol.Running
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
                IsEnabled = !_protocol.Running
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
                IsEnabled = !_protocol.Running
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

            _protocolRunningChangedAction = (o, running) =>
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    editLocalDataStoreButton.IsEnabled = createLocalDataStoreButton.IsEnabled = editRemoteDataStoreButton.IsEnabled = createRemoteDataStoreButton.IsEnabled = !running;
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

            _protocol.ProtocolRunningChanged += _protocolRunningChangedAction;
        }

        private async void CreateDataStore(bool local)
        {
            Type dataStoreType = local ? typeof(LocalDataStore) : typeof(RemoteDataStore);

            List<DataStore> dataStores = Assembly.GetExecutingAssembly()
                                                 .GetTypes()
                                                 .Where(t => !t.IsAbstract && t.IsSubclassOf(dataStoreType))
                                                 .Select(t => Activator.CreateInstance(t))
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

            _protocol.ProtocolRunningChanged -= _protocolRunningChangedAction;
        }
    }
}