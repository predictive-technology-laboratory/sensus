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
using Sensus.Probes.Location;
using Sensus.UI.Inputs;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class EstimoteBeaconProbeLocationsPage : ContentPage
    {
        public EstimoteBeaconProbeLocationsPage(EstimoteBeaconProbe probe)
        {
            Title = "Locations";

            ListView locationList = new ListView(ListViewCachingStrategy.RecycleElement);
            locationList.ItemTemplate = new DataTemplate(typeof(TextCell));
            locationList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            locationList.ItemsSource = probe.Locations;
            locationList.ItemTapped += async (o, e) =>
            {
                if (locationList.SelectedItem == null)
                {
                    return;
                }

                EstimoteLocation selectedLocation = locationList.SelectedItem as EstimoteLocation;

                string selectedAction = await DisplayActionSheet(selectedLocation.ToString(), "Cancel", null, "Delete");

                if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Confirm Delete", "Are you sure you want to delete the selected location?", "Yes", "Cancel"))
                    {
                        probe.Locations.Remove(selectedLocation);
                        locationList.SelectedItem = null;  // must reset this, since it isn't reset automatically
                    }
                }
            };

            Content = locationList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
            {
                EstimoteBeaconProbe estimoteBeaconProbe = probe as EstimoteBeaconProbe;

                List<EstimoteLocation> locations;
                try
                {
                    locations = estimoteBeaconProbe.GetLocationsFromCloud();

                    if (locations.Count == 0)
                    {
                        throw new Exception("No locations present within Estimote Cloud.");
                    }
                }
                catch (Exception ex)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Cannot add location:  " + ex);
                    return;
                }

                List<Input> inputs = await SensusServiceHelper.Get().PromptForInputsAsync("Add Location", new Input[]
                {
                    new ItemPickerDialogInput("Location:", null, locations.Select(location => location.Name + " (" + location.Identifier + ")").ToList())
                    {
                        AllowClearSelection = false
                    }

                }, null, true, null, null, null, null, false);

                if (inputs != null)
                {
                    try
                    {
                        string locationIdentifier = inputs[0].Value.ToString().Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)[1];
                        estimoteBeaconProbe.Locations.Add(locations.Single(location => location.Identifier == locationIdentifier));
                    }
                    catch (Exception)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Failed to add location.");
                    }
                }
            }));
        }
    }
}