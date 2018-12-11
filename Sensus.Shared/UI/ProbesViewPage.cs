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
