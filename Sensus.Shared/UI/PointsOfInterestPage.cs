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

using Xamarin.Forms;
using Xamarin.Forms.Maps;
using System;
using System.Threading;
using System.Collections.Generic;
using Sensus.Context;
using Sensus.UI.Inputs;
using Sensus.Probes.Location;
using Sensus.Concurrent;

namespace Sensus.UI
{
    /// <summary>
    /// Displays points of interest, allowing the user to add/delete them.
    /// </summary>
    public class PointsOfInterestPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointsOfInterestPage"/> class.
        /// </summary>
        /// <param name="pointsOfInterest">Points of interest to display.</param>        
        public PointsOfInterestPage(ConcurrentObservableCollection<PointOfInterest> pointsOfInterest)
        {
            Title = "Points of Interest";

            ListView pointsOfInterestList = new ListView(ListViewCachingStrategy.RecycleElement);
            pointsOfInterestList.ItemTemplate = new DataTemplate(typeof(TextCell));
            pointsOfInterestList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            pointsOfInterestList.ItemsSource = pointsOfInterest;
            pointsOfInterestList.ItemTapped += async (o, e) =>
            {
                if (pointsOfInterestList.SelectedItem == null)
                {
                    return;
                }

                PointOfInterest selectedPointOfInterest = pointsOfInterestList.SelectedItem as PointOfInterest;

                string selectedAction = await DisplayActionSheet(selectedPointOfInterest.ToString(), "Cancel", null, "Delete");

                if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedPointOfInterest.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        pointsOfInterest.Remove(selectedPointOfInterest);
                        pointsOfInterestList.SelectedItem = null;  // reset it manually, since it isn't done automatically.
                    }
                }
            };

            Content = pointsOfInterestList;

            CancellationTokenSource gpsCancellationTokenSource = null;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
            {
                List<Input> inputs = await SensusServiceHelper.Get().PromptForInputsAsync("Define Point Of Interest", new Input[]
                {
                    new SingleLineTextInput("POI Name:", Keyboard.Text) { Required = false },
                    new SingleLineTextInput("POI Type:", Keyboard.Text) { Required = false },
                    new SingleLineTextInput("Address:", Keyboard.Text) { Required = false }

                }, null, true, null, null, null, null, false);

                if (inputs == null)
                {
                    return;
                }

                string name = inputs[0].Value as string;
                string type = inputs[1].Value as string;
                string address = inputs[2].Value as string;

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(type))
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("You must enter either a name or type (or both).");
                }
                else
                {
                    Action<List<Position>> addPOI = new Action<List<Position>>(poiPositions =>
                    {
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            if (poiPositions != null && poiPositions.Count > 0 && await DisplayAlert("Add POI?", "Would you like to add " + poiPositions.Count + " point(s) of interest?", "Yes", "No"))
                            {
                                foreach (Position poiPosition in poiPositions)
                                {
                                    pointsOfInterest.Add(new PointOfInterest(name, type, poiPosition.ToGeolocationPosition()));
                                }
                            }
                        });
                    });

                    string newPinName = name + (string.IsNullOrWhiteSpace(type) ? "" : " (" + type + ")");

                    if (string.IsNullOrWhiteSpace(address))
                    {
                        // cancel existing token source if we have one
                        if (gpsCancellationTokenSource != null && !gpsCancellationTokenSource.IsCancellationRequested)
                        {
                            gpsCancellationTokenSource.Cancel();
                        }

                        gpsCancellationTokenSource = new CancellationTokenSource();

                        Plugin.Geolocator.Abstractions.Position gpsPosition = GpsReceiver.Get().GetReading(gpsCancellationTokenSource.Token, true);

                        if (gpsPosition != null)
                        {
                            SensusServiceHelper.Get().GetPositionsFromMapAsync(gpsPosition.ToFormsPosition(), newPinName, addPOI);
                        }
                    }
                    else
                    {
                        SensusServiceHelper.Get().GetPositionsFromMapAsync(address, newPinName, addPOI);
                    }
                }
            }));

            Disappearing += (o, e) =>
            {
                if (gpsCancellationTokenSource != null && !gpsCancellationTokenSource.IsCancellationRequested)
                {
                    gpsCancellationTokenSource.Cancel();
                }
            };
        }
    }
}