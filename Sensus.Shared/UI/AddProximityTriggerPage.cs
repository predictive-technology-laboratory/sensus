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
using System.Linq;
using System.Collections.Generic;
using Xamarin.Forms;
using Sensus.Probes.Location;

namespace Sensus.UI
{
    /// <summary>
    /// Allows the user to add a proximity trigger to a proximity probe.
    /// </summary>
    public class AddProximityTriggerPage : ContentPage
    {
        private IPointsOfInterestProximityProbe _proximityProbe;
        private string _pointOfInterestName;
        private string _pointOfInterestType;
        private double _distanceThresholdMeters;
        private ProximityThresholdDirection _thresholdDirection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddProximityTriggerPage"/> class.
        /// </summary>
        /// <param name="proximityProbe">Proximity probe to add trigger to.</param>
        public AddProximityTriggerPage(IPointsOfInterestProximityProbe proximityProbe)
        {
            _proximityProbe = proximityProbe;

            Title = "Add Trigger";

            List<PointOfInterest> pointsOfInterest = SensusServiceHelper.Get().PointsOfInterest.Union(_proximityProbe.Protocol.PointsOfInterest).ToList();
            if (pointsOfInterest.Count == 0)
            {
                Content = new Label
                {
                    Text = "No points of interest defined. Please define one or more before creating triggers.",
                    FontSize = 20
                };

                return;
            }

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            #region point of interest
            Label pointOfInterestLabel = new Label
            {
                Text = "POI:",
                FontSize = 20
            };

            Picker pointOfInterestPicker = new Picker
            {
                Title = "Select POI",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            foreach (string poiDesc in pointsOfInterest.Select(poi => poi.ToString()))
                pointOfInterestPicker.Items.Add(poiDesc);

            pointOfInterestPicker.SelectedIndexChanged += (o, e) =>
            {
                if (pointOfInterestPicker.SelectedIndex < 0)
                    _pointOfInterestName = _pointOfInterestType = null;
                else
                {
                    PointOfInterest poi = pointsOfInterest[pointOfInterestPicker.SelectedIndex];
                    _pointOfInterestName = poi.Name;
                    _pointOfInterestType = poi.Type;
                }
            };

            contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { pointOfInterestLabel, pointOfInterestPicker }
                });
            #endregion

            #region distance threshold
            Label distanceThresholdLabel = new Label
            {
                Text = "Distance Threshold (Meters):",
                FontSize = 20
            };

            Entry distanceThresholdEntry = new Entry
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Keyboard = Keyboard.Numeric
            };

            distanceThresholdEntry.TextChanged += (o, e) =>
            {
                if (!double.TryParse(distanceThresholdEntry.Text, out _distanceThresholdMeters))
                    _distanceThresholdMeters = -1;
                else if (_distanceThresholdMeters < GpsReceiver.Get().MinimumDistanceThreshold)
                    SensusServiceHelper.Get().FlashNotificationAsync("Distance threshold must be at least " + GpsReceiver.Get().MinimumDistanceThreshold + ".");
            };

            contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { distanceThresholdLabel, distanceThresholdEntry }
                });
            #endregion

            #region threshold direction
            Label thresholdDirectionLabel = new Label
            {
                Text = "Threshold Direction:",
                FontSize = 20
            };

            ProximityThresholdDirection[] thresholdDirections = new ProximityThresholdDirection[]{ ProximityThresholdDirection.Within, ProximityThresholdDirection.Outside };
            Picker thresholdDirectionPicker = new Picker
            {
                Title = "Select Threshold Direction",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            foreach (ProximityThresholdDirection thresholdDirection in thresholdDirections)
                thresholdDirectionPicker.Items.Add(thresholdDirection.ToString());

            thresholdDirectionPicker.SelectedIndexChanged += (o, e) =>
            {
                if (thresholdDirectionPicker.SelectedIndex < 0)
                    _thresholdDirection = ProximityThresholdDirection.Within;
                else
                    _thresholdDirection = thresholdDirections[thresholdDirectionPicker.SelectedIndex];
            };

            contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { thresholdDirectionLabel, thresholdDirectionPicker }
                });
            #endregion

            Button okButton = new Button
            {
                Text = "OK",
                FontSize = 20
            };

            okButton.Clicked += async (o, e) =>
            {
                try
                {
                    _proximityProbe.Triggers.Add(new PointOfInterestProximityTrigger(_pointOfInterestName, _pointOfInterestType, _distanceThresholdMeters, _thresholdDirection));
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    string message = "Failed to add trigger:  " + ex.Message;
                    SensusServiceHelper.Get().FlashNotificationAsync(message);
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                }
            };

            contentLayout.Children.Add(okButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
