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
using Xamarin.Forms.Maps;
using Xam.Plugin.MapExtend.Abstractions;
using System.Collections.ObjectModel;

namespace SensusUI
{
    public class MapPage : ContentPage
    {
        public MapPage(Position position, string newPinName)
        {            
            MapExtend map = new MapExtend(
                          MapSpan.FromCenterAndRadius(
                              position, Distance.FromMiles(0.3)))
            {
                IsShowingUser = true,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            ((ObservableCollection<Pin>)map.Pins).CollectionChanged += (o, e) =>
            {
                if (e.NewItems != null)
                    foreach (Pin pin in e.NewItems)
                        pin.Label = newPinName;
            };

            #region search
            Label searchLabel = new Label
            {
                Text = "Address:",
                FontSize = 20
            };

            Entry searchEntry = new Entry
            {
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            Button searchGoButton = new Button
            {
                Text = "Go",
                FontSize = 20
            };

            searchGoButton.Clicked += (o, e) =>
            {
                if (!string.IsNullOrWhiteSpace(searchEntry.Text))
                    map.SearchAdress(searchEntry.Text);
            };
            
            StackLayout searchStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children =
                {
                    searchLabel,
                    searchEntry,
                    searchGoButton
                }
            };
            #endregion

            Button clearPinsButton = new Button
            {
                Text = "Clear Pins",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            clearPinsButton.Clicked += (o, e) =>
            {
                map.Pins.Clear();
            };
            
            StackLayout mapStack = new StackLayout
            { 
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { map, searchStack, clearPinsButton }
            };
            
            Content = mapStack;
        }
    }
}