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

using Sensus.Probes;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProbesEditPage : ProbesPage
    {
        public ProbesEditPage(Protocol protocol)
            : base(protocol, "Edit Probes")
        {
            // enabling all probes is only available when the protocol is stopped. the enable is an async operation, and 
            // the probes don't play nice with each other when starting concurrently.
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

        protected override async void ProbeTapped(object sender, ItemTappedEventArgs e)
        {
            ProbePage probePage = new ProbePage(e.Item as Probe);
            await Navigation.PushAsync(probePage);
            ProbesList.SelectedItem = null;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            foreach (Probe probe in Protocol.Probes)
            {
                probe.OriginallyEnabled = probe.Enabled;
            }
        }
    }
}
