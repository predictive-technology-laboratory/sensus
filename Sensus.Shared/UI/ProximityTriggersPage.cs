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

using System.Linq;
using Sensus;
using Sensus.Probes.Location;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Displays proximity triggers.
    /// </summary>
    public class ProximityTriggersPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SensusUI.ProximityTriggersPage"/> class.
        /// </summary>
        /// <param name="proximityProbe">Proximity probe to display triggers for.</param>
        public ProximityTriggersPage(IPointsOfInterestProximityProbe proximityProbe)
        {
            Title = "Proximity Triggers";

            ListView triggerList = new ListView();
            triggerList.ItemTemplate = new DataTemplate(typeof(TextCell));
            triggerList.ItemTemplate.SetBinding(TextCell.TextProperty, new Binding(".", stringFormat: "{0}"));
            triggerList.ItemsSource = proximityProbe.Triggers;
            triggerList.ItemTapped += async (o, e) =>
            {
                if (triggerList.SelectedItem == null)
                {
                    return;
                }

                PointOfInterestProximityTrigger selectedTrigger = triggerList.SelectedItem as PointOfInterestProximityTrigger;

                string selectedAction = await DisplayActionSheet(selectedTrigger.ToString(), "Cancel", null, "Delete");

                if (selectedAction == "Delete")
                {
                    if (await DisplayAlert("Delete " + selectedTrigger + "?", "This action cannot be undone.", "Delete", "Cancel"))
                    {
                        proximityProbe.Triggers.Remove(selectedTrigger);
                        triggerList.SelectedItem = null;  // reset manually since it isn't done automatically
                    }
                }                        
            };

            Content = triggerList;

            ToolbarItems.Add(new ToolbarItem(null, "plus.png", async () =>
            {
                if (SensusServiceHelper.Get().PointsOfInterest.Union(proximityProbe.Protocol.PointsOfInterest).Count() > 0)
                {
                    await Navigation.PushAsync(new AddProximityTriggerPage(proximityProbe));
                }
                else
                {
                    SensusServiceHelper.Get().FlashNotificationAsync("You must define points of interest before adding triggers.");
                }
            }));
        }
    }
}