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
        private class EditDataStoreButtonTextValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return parameter + " store:  " + (value == null ? "None" : (value as DataStore).Name);
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        private class DataStoreButtonEnabledValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return !(bool)value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new SensusException("Invalid call to " + GetType().FullName + ".ConvertBack.");
            }
        }

        public static event EventHandler<ProtocolDataStoreEventArgs> EditDataStoreTapped;
        public static event EventHandler<ProtocolDataStoreEventArgs> CreateDataStoreTapped;
        public static event EventHandler<Protocol> ViewProbesTapped;
        public static event EventHandler<ProtocolReport> DisplayProtocolReport;

        private Protocol _protocol;

        public ProtocolPage(Protocol protocol)
        {
            _protocol = protocol;

            BindingContext = _protocol;

            SetBinding(TitleProperty, new Binding("Name"));

            List<View> views = new List<View>();

            views.AddRange(UiProperty.GetPropertyStacks(_protocol));

            #region data stores
            Button editLocalDataStoreButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20),
                BindingContext = _protocol
            };

            editLocalDataStoreButton.SetBinding(Button.TextProperty, new Binding("LocalDataStore", converter: new EditDataStoreButtonTextValueConverter(), converterParameter: "Local"));
            editLocalDataStoreButton.SetBinding(Button.IsEnabledProperty, new Binding("Running", converter: new DataStoreButtonEnabledValueConverter()));

            editLocalDataStoreButton.Clicked += (o, e) =>
                {
                    DataStore copy = null;
                    if (_protocol.LocalDataStore != null)
                        copy = _protocol.LocalDataStore.Copy();

                    EditDataStoreTapped(o, new ProtocolDataStoreEventArgs { Protocol = _protocol, DataStore = copy, Local = true });
                };

            Button createLocalDataStoreButton = new Button
            {
                Text = "+",
                HorizontalOptions = LayoutOptions.End,
                Font = Font.SystemFontOfSize(20),
                BindingContext = _protocol
            };

            createLocalDataStoreButton.SetBinding(Button.IsEnabledProperty, new Binding("Running", converter: new DataStoreButtonEnabledValueConverter()));

            createLocalDataStoreButton.Clicked += (o, e) =>
                {
                    CreateDataStoreTapped(o, new ProtocolDataStoreEventArgs { Protocol = _protocol, Local = true });
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
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20),
                BindingContext = _protocol
            };

            editRemoteDataStoreButton.SetBinding(Button.TextProperty, new Binding("RemoteDataStore", converter: new EditDataStoreButtonTextValueConverter(), converterParameter: "Remote"));
            editRemoteDataStoreButton.SetBinding(Button.IsEnabledProperty, new Binding("Running", converter: new DataStoreButtonEnabledValueConverter()));

            editRemoteDataStoreButton.Clicked += (o, e) =>
                {
                    DataStore copy = null;
                    if (_protocol.RemoteDataStore != null)
                        copy = _protocol.RemoteDataStore.Copy();

                    EditDataStoreTapped(o, new ProtocolDataStoreEventArgs { Protocol = _protocol, DataStore = copy, Local = false });
                };

            Button createRemoteDataStoreButton = new Button
            {
                Text = "+",
                HorizontalOptions = LayoutOptions.End,
                Font = Font.SystemFontOfSize(20),
                BindingContext = _protocol
            };

            createRemoteDataStoreButton.SetBinding(Button.IsEnabledProperty, new Binding("Running", converter: new DataStoreButtonEnabledValueConverter()));

            createRemoteDataStoreButton.Clicked += (o, e) =>
                {
                    CreateDataStoreTapped(o, new ProtocolDataStoreEventArgs { Protocol = _protocol, Local = false });
                };

            StackLayout remoteDataStoreStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { editRemoteDataStoreButton, createRemoteDataStoreButton }
            };

            views.Add(remoteDataStoreStack);
            #endregion

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
    }
}
