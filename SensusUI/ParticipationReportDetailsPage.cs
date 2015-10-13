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
using SensusService;
using Xamarin.Forms;
using SensusService.Probes;
using System.Linq;

namespace SensusUI
{
    public class ParticipationReportDetailsPage : ContentPage
    {
        public ParticipationReportDetailsPage(Protocol protocol)
        {
            Title = "Participation Details";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.StartAndExpand
            };

            foreach (Probe probe in protocol.Probes.Where(probe => probe.GetParticipation() != null).OrderBy(probe => probe.GetParticipation()))
            {
                float participation = probe.GetParticipation().GetValueOrDefault();

                contentLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        Children =
                        {
                            new Label
                            {
                                Text = probe.DisplayName + ":  " + Math.Round(participation * 100, 0) + "%",
                                FontSize = 20
                            },
                            new ProgressBar
                            {
                                Progress = participation
                            }
                        }
                    });
            }

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}