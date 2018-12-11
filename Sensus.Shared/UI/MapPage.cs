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
