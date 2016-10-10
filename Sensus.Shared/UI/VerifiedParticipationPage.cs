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
using Xamarin.Forms;
using SensusService;

namespace SensusUI
{
    public class VerifiedParticipationPage : ContentPage
    {
        public VerifiedParticipationPage(Protocol protocol, ParticipationRewardDatum participationRewardDatum)
        {
            Title = "Participation Verification";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 25, 0, 0)
            };

            string requiredParticipationPercentage = Math.Round(protocol.RewardThreshold.GetValueOrDefault() * 100, 0) + "%";
            string participationPercentage = Math.Round(participationRewardDatum.Participation * 100, 0) + "%";
            
            if (protocol.RewardThreshold == null)
            {
                contentLayout.Children.Add(
                    new Label
                    {
                        Text = "Verified Participation Level",
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    });

                contentLayout.Children.Add(
                    new Label
                    {
                        Text = participationPercentage,
                        FontSize = 50,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    }
                );
            }
            else
            {
                bool reward = participationRewardDatum.Participation >= protocol.RewardThreshold.GetValueOrDefault();

                contentLayout.Children.Add(
                    new Image
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Source = ImageSource.FromFile(reward ? "check.png" : "x.png")
                    });

                contentLayout.Children.Add(
                    new Label
                    {
                        Text = "Participant should " + (reward ? "" : "not ") + "be rewarded. This study requires " + requiredParticipationPercentage + " participation, and the participant is at " + participationPercentage + ".",
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    });
            }

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}