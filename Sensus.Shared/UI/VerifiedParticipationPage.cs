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
using Xamarin.Forms;
using Sensus;

namespace Sensus.UI
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
