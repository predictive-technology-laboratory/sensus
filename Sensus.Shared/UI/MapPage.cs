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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Xamarin;

namespace Sensus.UI
{
    public class MapPage : ContentPage
    {
        private Map _map;
        private Entry _searchEntry;

        public IList<Pin> Pins
        {
            get { return _map.Pins; }
        }

        private MapPage(string newPinName)
        {
            _map = new Map
            {
                IsShowingUser = true,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            ((ObservableCollection<Pin>)_map.Pins).CollectionChanged += (o, e) =>
            {
                // reset pin names to be the provided name
                if (e.NewItems != null)
                    foreach (Pin pin in e.NewItems)
                        pin.Label = newPinName;
            };

            #region search
            Label searchLabel = new Label
            {
                Text = "Search:",
                FontSize = 20
            };

            _searchEntry = new Entry
            {
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            Button searchGoButton = new Button
            {
                Text = "Go",
                FontSize = 20
            };

            searchGoButton.Clicked += async (o, e) =>
            {
                if (!string.IsNullOrWhiteSpace(_searchEntry.Text))
                {
                    try
                    {
                        // TODO:  Add this back
                        //_map.SearchAdress(_searchEntry.Text);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            string errorMessage = "Failed to search for address:  " + ex.Message;
                            SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                            await SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);
                        }
                        catch (Exception)
                        {
                        }
                    }
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
                _map.Pins.Clear();
            };

            Button okButton = new Button
            {
                Text = "OK",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            okButton.Clicked += async (o, e) =>
            {
                await Navigation.PopModalAsync();
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 20, 0, 0),
                Children =
                {
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { searchLabel, _searchEntry, searchGoButton }
                    },
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { clearPinsButton, okButton }
                    },
                    _map,
                }
            };
        }

        public MapPage(Position position, string newPinName)
            : this(newPinName)
        {
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMiles(0.3)));
            _map.Pins.Add(new Pin { Label = newPinName, Position = position });
        }

        public MapPage(string address, string newPinName)
            : this(newPinName)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                _searchEntry.Text = address.Trim();
                //_map.SearchAdress(_searchEntry.Text);
            }
        }
    }
}