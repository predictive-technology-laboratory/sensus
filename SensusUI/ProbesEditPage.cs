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

using SensusService;
using SensusService.Probes;
using Xamarin.Forms;

namespace SensusUI
{
    public class ProbesEditPage : ProbesPage
    {
        public ProbesEditPage(Protocol protocol)
            : base(protocol, "Edit Probes")
        {
            ToolbarItems.Add(new ToolbarItem("All", null, async () =>
                {
                    if (await DisplayAlert("Enable All Probes", "Are you sure you want to enable all probes?", "Yes", "No"))
                    {
                        foreach (Probe probe in Protocol.Probes)
                            if (SensusServiceHelper.Get().EnableProbeWhenEnablingAll(probe))
                                probe.Enabled = true;

                        Bind();
                    }
                }));

            ToolbarItems.Add(new ToolbarItem("None", null, async () =>
                {
                    if (await DisplayAlert("Disable All Probes", "Are you sure you want to disable all probes?", "Yes", "No"))
                    {
                        foreach (Probe probe in Protocol.Probes)
                            probe.Enabled = false;

                        Bind();
                    }
                }));
        }

        protected override async void ProbeTapped(object sender, ItemTappedEventArgs e)
        {
            ProbePage probePage = new ProbePage(e.Item as Probe);
            probePage.Disappearing += (oo, ee) => { Bind(); };  // rebind the probes page to pick up changes in the probe
            await Navigation.PushAsync(probePage);
            ProbesList.SelectedItem = null;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            foreach (Probe probe in Protocol.Probes)
                probe.OriginallyEnabled = probe.Enabled;
        }
    }
}