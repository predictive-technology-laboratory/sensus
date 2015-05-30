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
using Xamarin.Geolocation;

namespace SensusUI
{
    public class PointsOfInterestPage : ContentPage
    {
        private List<PointOfInterest> _pointsOfInterest;
        private ListView _pointsOfInterestList;

        public PointsOfInterestPage(List<PointOfInterest> pointsOfInterest, Action changeCallback)
        {
            _pointsOfInterest = pointsOfInterest;

            Title = "Points of Interest";

            _pointsOfInterestList = new ListView();
            _pointsOfInterestList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _pointsOfInterestList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            _pointsOfInterestList.ItemTapped += async (o, e) =>
            {
                    if(_pointsOfInterestList.SelectedItem == null)
                        return;

                    PointOfInterest selectedPointOfInterest = _pointsOfInterestList.SelectedItem as PointOfInterest;

                    string selectedAction = await DisplayActionSheet(selectedPointOfInterest.ToString(), "Cancel", null, "Delete");

                    if(selectedAction == "Delete")
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
                        UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Enter a name for the new point of interest:", false, name =>
                            {
                                UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Enter a type for the new point of interest:", false, type =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(type))
                                        {
                                            Position position = GpsReceiver.Get().GetReading(default(CancellationToken));
                                            if (position != null)
                                            {
                                                _pointsOfInterest.Add(new PointOfInterest(name, type, position));

                                                if (changeCallback != null)
                                                    changeCallback();
                                
                                                Bind();
                                            }
                                        }
                                    });
                            });
                    }));
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