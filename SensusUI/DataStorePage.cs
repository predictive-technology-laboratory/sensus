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

using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace SensusUI
{
    public class DataStorePage : ContentPage
    {        
        public static event EventHandler OkTapped;

        public DataStorePage(ProtocolDataStoreEventArgs args)
        {
            BindingContext = args.DataStore;

            SetBinding(TitleProperty, new Binding("Name"));

            List<StackLayout> stacks = UiProperty.GetPropertyStacks(args.DataStore);

            Button clearButton = new Button
            {
                Text = "Clear",
                HorizontalOptions = LayoutOptions.Start,
                Font = Font.SystemFontOfSize(20),
                IsEnabled = args.DataStore.Clearable
            };

            clearButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Clear data from " + args.DataStore.Name + "?", "This action cannot be undone.", "Clear", "Cancel"))
                        args.DataStore.Clear();
                };

            Button okayButton = new Button
            {
                Text = "OK",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            okayButton.Clicked += (o, e) =>
                {
                    if (args.Local)
                        args.Protocol.LocalDataStore = args.DataStore as LocalDataStore;
                    else
                        args.Protocol.RemoteDataStore = args.DataStore as RemoteDataStore;

                    OkTapped(o, e);
                };

            stacks.Add(new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { clearButton, okayButton }
            });

            StackLayout contentLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            foreach (StackLayout stack in stacks)
                contentLayout.Children.Add(stack);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
