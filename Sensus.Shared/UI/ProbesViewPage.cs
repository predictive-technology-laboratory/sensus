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
using Syncfusion.SfChart.XForms;
using Xamarin.Forms;
using System;
using System.Threading.Tasks;

namespace Sensus.UI
{
    public class ProbesViewPage : ProbesPage
    {
        public ProbesViewPage(Protocol protocol)
            : base(protocol, "View Data")
        {
        }

        protected override async void ProbeTapped(object sender, ItemTappedEventArgs e)
        {
            Probe probe = e.Item as Probe;

            SfChart chart = null;

            try
            {
                chart = probe.GetChart();
            }
            catch (NotImplementedException)
            {
            }

            if (chart == null)
            {
                await SensusServiceHelper.Get().FlashNotificationAsync("Charts are not available for " + probe.DisplayName + " data.");
            }
            else
            {
                ContentPage chartPage = new ContentPage
                {
                    Title = probe.DisplayName,
                    Content = chart,
                };

                chartPage.ToolbarItems.Add(new ToolbarItem("Refresh", null, () =>
                {
                    chartPage.Content = probe.GetChart();
                }));

                chartPage.ToolbarItems.Add(new ToolbarItem("+", null, async () =>
                {
                    if (probe.MaxChartDataCount < 200)
                    {
                        probe.MaxChartDataCount += 10;
                    }

                    await FlashChartDataCountAsync(probe);
                }));

                chartPage.ToolbarItems.Add(new ToolbarItem("-", null, async () =>
                {
                    if (probe.MaxChartDataCount > 10)
                    {
                        probe.MaxChartDataCount -= 10;
                    }

                    await FlashChartDataCountAsync(probe);
                }));

                await Navigation.PushAsync(chartPage);
            }
        }

        private async Task FlashChartDataCountAsync(Probe probe)
        {
            await SensusServiceHelper.Get().FlashNotificationAsync("Displaying " + probe.MaxChartDataCount + " point" + (probe.MaxChartDataCount == 1 ? "" : "s") + ".");
        }
    }
}