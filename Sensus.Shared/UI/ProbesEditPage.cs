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

using Sensus.Probes;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProbesEditPage : ProbesPage
    {
        public ProbesEditPage(Protocol protocol)
            : base(protocol, "Edit Probes")
        {
            // restore the enabled status of each probe to reflect its original values. this ensures that
            // probes disabled due to running without hardware support do not continue to be disabled 
            // indefinitely when dismissing this page (see OnDisappearing).
            foreach (Probe probe in protocol.Probes)
            {
                probe.Enabled = probe.OriginallyEnabled;
            }

            // enabling all probes is only available when the protocol is stopped. the enable is an async operation, and 
            // the probes don't play nice with each other when starting/stopping concurrently.
            if (protocol.State == ProtocolState.Stopped)
            {
                ToolbarItems.Add(new ToolbarItem("All", null, async () =>
                {
                    if (await DisplayAlert("Enable All Probes", "Are you sure you want to enable all probes?", "Yes", "No"))
                    {
                        foreach (Probe probe in Protocol.Probes)
                        {
                            if (SensusServiceHelper.Get().EnableProbeWhenEnablingAll(probe))
                            {
                                probe.Enabled = true;
                            }
                        }
                    }
                }));
            }

            ToolbarItems.Add(new ToolbarItem("None", null, async () =>
            {
                if (await DisplayAlert("Disable All Probes", "Are you sure you want to disable all probes?", "Yes", "No"))
                {
                    foreach (Probe probe in Protocol.Probes)
                    {
                        probe.Enabled = false;
                    }
                }
            }));
        }

        protected override async void ProbeTappedAsync(object sender, ItemTappedEventArgs e)
        {
            ProbePage probePage = new ProbePage(e.Item as Probe);
            await Navigation.PushAsync(probePage);
            ProbesList.SelectedItem = null;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // the user is finished editing probe settings. store the enabled status of each probe. we use this
            // value to restore the protocol prior to sharing, e.g., in cases where the probe has become disabled 
            // because it was started without hardware support.
            foreach (Probe probe in Protocol.Probes)
            {
                probe.OriginallyEnabled = probe.Enabled;
            }
        }
    }
}