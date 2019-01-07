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
using Sensus.Probes.Location;
using Sensus.Context;

#if __ANDROID__
using EstimoteIndoorLocationView = Estimote.Android.Indoor.IndoorLocationView;
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using EstimoteIndoorLocationView = Estimote.iOS.Indoor.EILIndoorLocationView;
using Xamarin.Forms.Platform.iOS;
#endif

namespace Sensus.UI
{
    public class ProbesViewPage : ProbesPage
    {
        public ProbesViewPage(Protocol protocol)
            : base(protocol, "View Data")
        {
        }

        protected override async void ProbeTappedAsync(object sender, ItemTappedEventArgs e)
        {
            Probe probe = e.Item as Probe;

            if (probe is EstimoteBeaconProbe)
            {
                EstimoteIndoorLocationView locationView;

#if __ANDROID__
                locationView = new EstimoteIndoorLocationView(global::Android.App.Application.Context);
#elif __IOS__
                locationView = new EstimoteIndoorLocationView
                {
                    PositionImage = UIKit.UIImage.FromFile("account.png")
                };
#endif

                ContentPage indoorLocationPage = new ContentPage
                {
                    Title = "Estimote Indoor Location",
                    Content = locationView.ToView()
                };

                await Navigation.PushAsync(indoorLocationPage);

                probe.MostRecentDatumChanged += (previous, current) =>
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        // indoor positioning will report null if the user is outside the area
                        if (current == null)
                        {
                            indoorLocationPage.Content.IsVisible = false;
                        }
                        else
                        {
                            indoorLocationPage.Content.IsVisible = true;

                            EstimoteIndoorLocationDatum currentEstimoteDatum = current as EstimoteIndoorLocationDatum;

#if __IOS__
                            // must draw location before updating position
                            locationView.DrawLocation(currentEstimoteDatum.EstimoteLocation);
#endif

                            locationView.UpdatePosition(currentEstimoteDatum.EstimotePosition);
                        }
                    });

                    return Task.CompletedTask;
                };
            }
            else
            {
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
        }

        private async Task FlashChartDataCountAsync(Probe probe)
        {
            await SensusServiceHelper.Get().FlashNotificationAsync("Displaying " + probe.MaxChartDataCount + " point" + (probe.MaxChartDataCount == 1 ? "" : "s") + ".");
        }
    }
}