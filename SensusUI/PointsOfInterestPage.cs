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
                                            _pointsOfInterest.Add(new PointOfInterest(name, type, GpsReceiver.Get().GetReading(default(CancellationToken))));

                                            if (changeCallback != null)
                                                changeCallback();
                                
                                            Bind();
                                        }
                                    });
                            });
                    }));

            ToolbarItems.Add(new ToolbarItem(null, "minus.png", async () =>
                    {
                        if (_pointsOfInterestList.SelectedItem != null)
                        {
                            PointOfInterest pointOfInterestToDelete = _pointsOfInterestList.SelectedItem as PointOfInterest;

                            if (await DisplayAlert("Delete " + pointOfInterestToDelete.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                            {
                                _pointsOfInterest.Remove(pointOfInterestToDelete);
                                
                                if (changeCallback != null)
                                    changeCallback();

                                Bind();
                            }
                        }
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