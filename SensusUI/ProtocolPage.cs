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
using SensusService.DataStores;
using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;
using SensusUI.Inputs;

namespace SensusUI
{
    /// <summary>
    /// Displays a single protocol.
    /// </summary>
    public class ProtocolPage : ContentPage
    {
        private Protocol _protocol;
        private EventHandler<bool> _protocolRunningChangedAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensusUI.ProtocolPage"/> class.
        /// </summary>
        /// <param name="protocol">Protocol to display.</param>
        public ProtocolPage(Protocol protocol)
        {
            _protocol = protocol;

            Title = "Protocol";

            List<View> views = new List<View>();

            views.AddRange(UiProperty.GetPropertyStacks(_protocol));

            #region data stores
            string localDataStoreSize = null;
            try
            {
                if (protocol.LocalDataStore != null)
                {
                    if (protocol.LocalDataStore is RamLocalDataStore)
                        localDataStoreSize = (protocol.LocalDataStore as RamLocalDataStore).DataCount + " items";
                    else if (protocol.LocalDataStore is FileLocalDataStore)
                        localDataStoreSize = Math.Round(SensusServiceHelper.GetDirectorySizeMB((protocol.LocalDataStore as FileLocalDataStore).StorageDirectory), 1) + " MB";
                }
            }
            catch (Exception)
            {
            }

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
                    await Navigation.PushAsync(new DataStorePage(_protocol, _protocol.LocalDataStore.Copy(), true, false));
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
                    await Navigation.PushAsync(new DataStorePage(_protocol, _protocol.RemoteDataStore.Copy(), false, false));
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

            _protocolRunningChangedAction = (o, running) =>
            {
                Device.BeginInvokeOnMainThread(() =>
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
                stack.Children.Add(view);

            Button lockButton = new Button
            {
                Text = _protocol.LockPasswordHash == "" ? "Lock" : "Unlock",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            lockButton.Clicked += (o, e) =>
            {
                if (lockButton.Text == "Lock")
                {
                    SensusServiceHelper.Get().PromptForInputAsync(
                        "Lock Protocol",
                        new SingleLineTextInput("Password:", Keyboard.Text, true),
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
                                return;

                            string password = input.Value as string;

                            if (string.IsNullOrWhiteSpace(password))
                                SensusServiceHelper.Get().FlashNotificationAsync("Please enter a non-empty password.");
                            else
                            {
                                _protocol.LockPasswordHash = SensusServiceHelper.Get().GetHash(password);
                                Device.BeginInvokeOnMainThread(() => lockButton.Text = "Unlock");
                            }
                        });
                }
                else if (lockButton.Text == "Unlock")
                {
                    _protocol.LockPasswordHash = "";
                    lockButton.Text = "Lock";
                }
            };

            stack.Children.Add(lockButton);

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
                await Navigation.PushAsync(new DataStorePage(_protocol, dataStores[int.Parse(selected.Substring(0, selected.IndexOf(")"))) - 1], local, true));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _protocol.ProtocolRunningChanged -= _protocolRunningChangedAction;
        }
    }
}