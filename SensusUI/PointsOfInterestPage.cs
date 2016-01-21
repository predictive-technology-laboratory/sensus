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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SensusService;
using SensusService.Probes.Location;
using Xamarin.Forms;
using SensusUI.Inputs;
using Xamarin.Forms.Maps;

namespace SensusUI
{
    /// <summary>
    /// Displays points of interest, allowing the user to add/delete them.
    /// </summary>
    public class PointsOfInterestPage : ContentPage
    {
        private List<PointOfInterest> _pointsOfInterest;
        private ListView _pointsOfInterestList;
        private CancellationTokenSource _gpsCancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensusUI.PointsOfInterestPage"/> class.
        /// </summary>
        /// <param name="pointsOfInterest">Points of interest to display.</param>
        /// <param name="changeCallback">Called when a POI is added/deleted.</param>
        public PointsOfInterestPage(List<PointOfInterest> pointsOfInterest, Action changeCallback)
        {
            _pointsOfInterest = pointsOfInterest;

            Title = "Points of Interest";

            _pointsOfInterestList = new ListView();
            _pointsOfInterestList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _pointsOfInterestList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            _pointsOfInterestList.ItemTapped += async (o, e) =>
            {
                if (_pointsOfInterestList.SelectedItem == null)
                    return;

                PointOfInterest selectedPointOfInterest = _pointsOfInterestList.SelectedItem as PointOfInterest;

                string selectedAction = await DisplayActionSheet(selectedPointOfInterest.ToString(), "Cancel", null, "Delete");

                if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedPointOfInterest.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        _pointsOfInterest.Remove(selectedPointOfInterest);
                        _pointsOfInterestList.SelectedItem = null;  // reset it manually, since it isn't done automatically.

                        if (changeCallback != null)
                            changeCallback();

                        Bind();
                    }
                }
            };
            
            Bind();

            Content = _pointsOfInterestList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                    {
                        SensusServiceHelper.Get().PromptForInputsAsync(
                            "Define Point Of Interest", 
                            new Input[]
                            {
                                new TextInput("POI Name:", Keyboard.Text) { Required = false },
                                new TextInput("POI Type:", Keyboard.Text) { Required = false },
                                new TextInput("Address:", Keyboard.Text) { Required = false }
                            },
                            null,
                            true, 
                            null,
                            null,
                            null,
                            null,
                            false,
                            inputs =>
                            {
                                if (inputs == null)
                                    return;
                            
                                string name = inputs[0].Value as string;
                                string type = inputs[1].Value as string;
                                string address = inputs[2].Value as string;

                                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(type))
                                    SensusServiceHelper.Get().FlashNotificationAsync("You must enter either a name or type (or both).");
                                else
                                {
                                    Action<List<Position>> addPOI = new Action<List<Position>>(poiPositions =>
                                        {        
                                            Device.BeginInvokeOnMainThread(async () =>
                                                {
                                                    if (poiPositions != null && poiPositions.Count > 0 && await DisplayAlert("Add POI?", "Would you like to add " + poiPositions.Count + " point(s) of interest?", "Yes", "No"))
                                                        foreach (Position poiPosition in poiPositions)
                                                        {
                                                            _pointsOfInterest.Add(new PointOfInterest(name, type, poiPosition.ToGeolocationPosition()));

                                                            if (changeCallback != null)
                                                                changeCallback();

                                                            Bind();
                                                        }
                                                });
                                        });

                                    string newPinName = name + (string.IsNullOrWhiteSpace(type) ? "" : " (" + type + ")");

                                    if (string.IsNullOrWhiteSpace(address))
                                    {
                                        // cancel existing token source if we have one
                                        if (_gpsCancellationTokenSource != null && !_gpsCancellationTokenSource.IsCancellationRequested)
                                            _gpsCancellationTokenSource.Cancel();

                                        _gpsCancellationTokenSource = new CancellationTokenSource();
                                        
                                        Plugin.Geolocator.Abstractions.Position gpsPosition = GpsReceiver.Get().GetReading(_gpsCancellationTokenSource.Token);

                                        if (gpsPosition != null)
                                            SensusServiceHelper.Get().GetPositionsFromMapAsync(gpsPosition.ToFormsPosition(), newPinName, addPOI);
                                    }
                                    else
                                        SensusServiceHelper.Get().GetPositionsFromMapAsync(address, newPinName, addPOI);
                                }
                            });
                    }));

            Disappearing += (o, e) =>
            {
                if (_gpsCancellationTokenSource != null && !_gpsCancellationTokenSource.IsCancellationRequested)
                    _gpsCancellationTokenSource.Cancel();
            };
        }

        private void Bind()
        {
            Device.BeginInvokeOnMainThread(() =>
                {
                    _pointsOfInterestList.ItemsSource = null;
                    _pointsOfInterestList.ItemsSource = _pointsOfInterest;
                });
        }
    }
}