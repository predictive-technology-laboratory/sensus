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
using SensusService.Exceptions;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace SensusUI
{
    public class ProtocolPage : ContentPage
    {
        public static event EventHandler<ProtocolDataStoreEventArgs> EditDataStoreTapped;
        public static event EventHandler<ProtocolDataStoreEventArgs> CreateDataStoreTapped;
        public static event EventHandler<Protocol> ViewProbesTapped;
        public static event EventHandler<ProtocolReport> DisplayProtocolReport;

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
                Font = Font.SystemFontOfSize(20),
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
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                IsEnabled = !_protocol.Running
            };

            editLocalDataStoreButton.Clicked += (o, e) =>
                {
                    DataStore copy = null;
                    if (_protocol.LocalDataStore != null)
                        copy = _protocol.LocalDataStore.Copy();

                    EditDataStoreTapped(this, new ProtocolDataStoreEventArgs { Protocol = _protocol, DataStore = copy, Local = true });
                };

            Button createLocalDataStoreButton = new Button
            {
                Text = "+",
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.End,
                IsEnabled = !_protocol.Running
            };

            createLocalDataStoreButton.Clicked += (o, e) =>
                {
                    CreateDataStoreTapped(this, new ProtocolDataStoreEventArgs { Protocol = _protocol, Local = true });
                };

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
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                IsEnabled = !_protocol.Running
            };

            editRemoteDataStoreButton.Clicked += (o, e) =>
                {
                    DataStore copy = null;
                    if (_protocol.RemoteDataStore != null)
                        copy = _protocol.RemoteDataStore.Copy();

                    EditDataStoreTapped(this, new ProtocolDataStoreEventArgs { Protocol = _protocol, DataStore = copy, Local = false });
                };

            Button createRemoteDataStoreButton = new Button
            {
                Text = "+",
                Font = Font.SystemFontOfSize(20),
                HorizontalOptions = LayoutOptions.End,
                IsEnabled = !_protocol.Running
            };

            createRemoteDataStoreButton.Clicked += (o, e) =>
                {
                    CreateDataStoreTapped(this, new ProtocolDataStoreEventArgs { Protocol = _protocol, Local = false });
                };

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
                Font = Font.SystemFontOfSize(20)
            };

            viewProbesButton.Clicked += (o, e) =>
                {
                    ViewProbesTapped(o, _protocol);
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
                        await _protocol.PingAsync();

                        if (_protocol.MostRecentReport == null)
                            await DisplayAlert("No Report", "Ping failed.", "OK");
                        else
                            DisplayProtocolReport(this, _protocol.MostRecentReport);
                    }
                    else
                        await DisplayAlert("Protocol Not Running", "Cannot ping protocol when it is not running.", "OK");
                }));

            ToolbarItems.Add(new ToolbarItem("Share", null, () =>
                {
                    string path = null;
                    try
                    {
                        path = UiBoundSensusServiceHelper.Get().GetSharePath(".sensus");
                        _protocol.Save(path);
                    }
                    catch (Exception ex)
                    {
                        UiBoundSensusServiceHelper.Get().Logger.Log("Failed to save protocol to file for sharing:  " + ex.Message, LoggingLevel.Normal);
                        path = null;
                    }

                    if (path != null)
                        UiBoundSensusServiceHelper.Get().ShareFileAsync(path, "Sensus Protocol:  " + _protocol.Name);
                }));
            #endregion
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _protocol.ProtocolRunningChanged += _protocolRunningChangedAction;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _protocol.ProtocolRunningChanged -= _protocolRunningChangedAction;

            UiBoundSensusServiceHelper.Get().SaveRegisteredProtocols();
        }
    }
}
