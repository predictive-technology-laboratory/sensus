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
using System.Threading.Tasks;
using Sensus.Probes.Location;
using Sensus.UI.Inputs;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class EstimoteBeaconProbeBeaconsPage : ContentPage
    {
        public EstimoteBeaconProbeBeaconsPage(EstimoteBeaconProbe probe)
        {
            Title = "Beacons";

            ListView beaconList = new ListView();
            beaconList.ItemTemplate = new DataTemplate(typeof(TextCell));
            beaconList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            beaconList.ItemsSource = probe.Beacons;
            beaconList.ItemTapped += async (o, e) =>
            {
                if (beaconList.SelectedItem == null)
                {
                    return;
                }

                EstimoteBeacon selectedBeacon = beaconList.SelectedItem as EstimoteBeacon;

                string selectedAction = await DisplayActionSheet(selectedBeacon.ToString(), "Cancel", null, "Delete");

                if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Confirm Delete", "Are you sure you want to delete the selected beacon?", "Yes", "Cancel"))
                    {
                        probe.Beacons.Remove(selectedBeacon);
                        beaconList.SelectedItem = null;  // must reset this, since it isn't reset automatically
                    }
                }
            };

            Content = beaconList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
            {
                await Task.Run(() =>
                {
                    EstimoteBeaconProbe estimoteBeaconProbe = probe as EstimoteBeaconProbe;

                    List<string> beacons;
                    try
                    {
                        beacons = estimoteBeaconProbe.GetSensusBeaconNamesFromCloud();
                        if (beacons == null)
                        {
                            throw new Exception("No beacons");
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().FlashNotificationAsync("Failed to retrieve Estimote beacons from Cloud:  " + ex);
                        return;
                    }

                    SensusServiceHelper.Get().PromptForInputsAsync("Add Beacon", new Input[]
                    {
                        new ItemPickerDialogInput("Beacon Name:", null, beacons)
                        {
                            AllowClearSelection = false
                        },
                        new NumberEntryInput("Proximity (Meters):"),
                        new SingleLineTextInput("Event Name (Defaults To Beacon Name):", Keyboard.Default)
                        {
                            Required = false
                        }
                    },
                    null, true, null, null, null, null, false, (inputs) =>
                    {
                        if (inputs != null)
                        {
                            try
                            {
                                string beaconName = inputs[0].Value.ToString();
                                double beaconProximity = double.Parse(inputs[1].Value.ToString());
                                string eventName = inputs[2].Value?.ToString();
                                EstimoteBeacon beacon = new EstimoteBeacon(beaconName, beaconProximity, eventName);
                                estimoteBeaconProbe.Beacons.Add(beacon);
                            }
                            catch (Exception)
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("Failed to add beacon.");
                            }
                        }
                    });
                });
            }));
        }
    }
}