#region copyright
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
#endregion

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
                        await Navigation.PushAsync(new DataStorePage(_protocol, _protocol.LocalDataStore.Copy(), true));
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
                        await Navigation.PushAsync(new DataStorePage(_protocol, _protocol.RemoteDataStore.Copy(), false));
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

            Content = new ScrollView
            {
                Content = stack
            };

            #region toolbar
            ToolbarItems.Add(new ToolbarItem("Ping", null, async () =>
                {
                    if (SensusServiceHelper.Get().ProtocolShouldBeRunning(_protocol))
                    {
                        _protocol.PingAsync( () =>
                            {
								Device.BeginInvokeOnMainThread(async () =>
									{
										if (_protocol.MostRecentReport == null)
											await DisplayAlert("No Report", "Ping failed.", "OK");
										else
											await Navigation.PushAsync(new ViewTextLinesPage("Protocol Report", _protocol.MostRecentReport.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(), null));
									});
							});
					}
                    else
                        await DisplayAlert("Protocol Not Running", "Cannot ping protocol when it is not running.", "OK");
                }));

            ToolbarItems.Add(new ToolbarItem("Share", null, () =>
                {
                    string path = null;
                    try
                    {
                        path = UiBoundSensusServiceHelper.Get(true).GetSharePath(".sensus");
                        _protocol.Save(path);
                    }
                    catch (Exception ex)
                    {
                        UiBoundSensusServiceHelper.Get(true).Logger.Log("Failed to save protocol to file for sharing:  " + ex.Message, LoggingLevel.Normal);
                        path = null;
                    }

                    if (path != null)
                        UiBoundSensusServiceHelper.Get(true).ShareFileAsync(path, "Sensus Protocol:  " + _protocol.Name);
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
                                                 .ToList();

            string selected = await DisplayActionSheet("Select " + (local ? "Local" : "Remote") + " Data Store", "Cancel", null, dataStores.Select((d, i) => (i + 1) + ") " + d.Name).ToArray());
            if (selected != null)
                await Navigation.PushAsync(new DataStorePage(_protocol, dataStores[int.Parse(selected.Substring(0, selected.IndexOf(")"))) - 1], local));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _protocol.ProtocolRunningChanged -= _protocolRunningChangedAction;
        }
    }
}
