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

namespace SensusUI
{
    public class ProtocolPage : ContentPage
    {
        private Protocol _protocol;
        private EventHandler<bool> _protocolRunningChangedAction;

        public ProtocolPage(Protocol protocol)
        {
            _protocol = protocol;

            Title = "Protocol";

            List<View> views = new List<View>();

            #region on/off
            Label onOffLabel = new Label
            {
                Text = "Status:",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.Start
            };

            Switch onOffSwitch = new Switch
            {
                IsToggled = _protocol.Running
            };

            onOffSwitch.Toggled += (o, e) => _protocol.Running = e.Value;

            views.Add(new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { onOffLabel, onOffSwitch }
            });
            #endregion

            views.AddRange(UiProperty.GetPropertyStacks(_protocol));

            #region data stores
            Button editLocalDataStoreButton = new Button
            {
                Text = "Local Data Store",
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

            #region view probes
            Button viewProbesButton = new Button
            {
                Text = "Probes",
                FontSize = 20
            };

            viewProbesButton.Clicked += async (o, e) =>
                {
                    await Navigation.PushAsync(new ProbesPage(_protocol));
                };

            views.Add(viewProbesButton);
            #endregion

            _protocolRunningChangedAction = (o, running) =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                        {
                            onOffSwitch.IsToggled = running;
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
                    UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Create password to lock protocol:", false, password =>
                        {
                            if (password == null)
                                return;
                            else if (string.IsNullOrWhiteSpace(password))
                                UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Please enter a non-empty password.");
                            else
                            {
                                _protocol.LockPasswordHash = UiBoundSensusServiceHelper.Get(true).GetMd5Hash(password);
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

            #region toolbar            
            ToolbarItems.Add(new ToolbarItem("Status", null, async () =>
                {
                    if (UiBoundSensusServiceHelper.Get(true).ProtocolShouldBeRunning(_protocol))
                    {
                        _protocol.TestHealthAsync(() =>
                            {
								Device.BeginInvokeOnMainThread(async () =>
									{
										if (_protocol.MostRecentReport == null)
											await DisplayAlert("No Report", "Status check failed.", "OK");
										else
											await Navigation.PushAsync(new ViewTextLinesPage("Protocol Report", _protocol.MostRecentReport.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(), null));
									});
                            });
					}
                    else
                        await DisplayAlert("Protocol Not Running", "Cannot check status of protocol when protocol is not running.", "OK");
                }));                    
            #endregion
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
                                                 .OrderBy(d => d.Name)
                                                 .ToList();

            string cancelButtonName = "Cancel";
            string selected = await DisplayActionSheet("Select " + (local ? "Local" : "Remote") + " Data Store", cancelButtonName, null, dataStores.Select((d, i) => (i + 1) + ") " + d.Name).ToArray());
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
