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
using SensusService.Probes.Location;
using SensusService;
using System.Linq;
using System.Threading;

namespace SensusUI
{
    public class PointsOfInterestPage : ContentPage
    {
        private ListView _pointsOfInterestList;

        public PointsOfInterestPage()
        {
            Title = "Points Of Interest";

            _pointsOfInterestList = new ListView();
            _pointsOfInterestList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _pointsOfInterestList.ItemTemplate.SetBinding(TextCell.TextProperty, "Name");

            Bind();

            Content = _pointsOfInterestList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", () =>
                    {
                        UiBoundSensusServiceHelper.Get(true).PromptForInputAsync("Enter a name for this point of interest:", false, input =>
                            {
                                if (!string.IsNullOrWhiteSpace(input))
                                {
                                    UiBoundSensusServiceHelper.Get(true).PointsOfInterest.Add(new PointOfInterest(input, GpsReceiver.Get().GetReading(default(CancellationToken))));
                                    UiBoundSensusServiceHelper.Get(true).SaveAsync();

                                    Bind();
                                }
                            });
                    }));

            ToolbarItems.Add(new ToolbarItem(null, "minus.png", async () =>
                    {
                        if (_pointsOfInterestList.SelectedItem != null)
                        {
                            PointOfInterest pointOfInterestToDelete = _pointsOfInterestList.SelectedItem as PointOfInterest;

                            if (await DisplayAlert("Delete " + pointOfInterestToDelete.Name + "?", "This action cannot be undone.", "Delete", "Cancel"))
                            {
                                UiBoundSensusServiceHelper.Get(true).PointsOfInterest.Remove(pointOfInterestToDelete);
                                UiBoundSensusServiceHelper.Get(true).SaveAsync();

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
                    _pointsOfInterestList.ItemsSource = UiBoundSensusServiceHelper.Get(true).PointsOfInterest;
                });
        }
    }
}

