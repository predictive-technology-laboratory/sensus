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

            ListView beaconList = new ListView(ListViewCachingStrategy.RecycleElement);
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
                EstimoteBeaconProbe estimoteBeaconProbe = probe as EstimoteBeaconProbe;

                List<string> beaconTags;
                try
                {
                    beaconTags = estimoteBeaconProbe.GetBeaconTagsFromCloud();

                    if (beaconTags.Count == 0)
                    {
                        throw new Exception("No beacons with tags present within Estimote Cloud.");
                    }
                }
                catch (Exception ex)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Cannot add beacon:  " + ex);
                    return;
                }

                List<Input> inputs = await SensusServiceHelper.Get().PromptForInputsAsync("Add Beacon", new Input[]
                {
                    new ItemPickerDialogInput("Beacon Tag:", null, beaconTags)
                    {
                        AllowClearSelection = false
                    },
                    new NumberEntryInput("Proximity (Meters):"),
                    new SingleLineTextInput("Event Name (Defaults To Beacon Tag):", Keyboard.Default)
                    {
                            Required = false
                    }

                }, null, true, null, null, null, null, false);

                if (inputs != null)
                {
                    try
                    {
                        string beaconTag = inputs[0].Value.ToString();
                        double beaconProximity = double.Parse(inputs[1].Value.ToString());
                        string eventName = inputs[2].Value?.ToString();
                        EstimoteBeacon beacon = new EstimoteBeacon(beaconTag, beaconProximity, eventName);
                        estimoteBeaconProbe.Beacons.Add(beacon);
                    }
                    catch (Exception)
                    {
                        await SensusServiceHelper.Get().FlashNotificationAsync("Failed to add beacon.");
                    }
                }
            }));
        }
    }
}
