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
    public class ParticipationReportPage : ContentPage
    {
        public ParticipationReportPage(Protocol protocol, string participationRewardDatumId)
        {
            Title = protocol.Name;

            #if __IOS__
            string howToIncreaseScore = "You can increase your score by opening Sensus more often and responding to questions that Sensus asks you.";
            #elif __ANDROID__
            string howToIncreaseScore = "You can increase your score by allowing Sensus to run continuously and responding to questions that Sensus asks you.";
            #elif WINDOWS_PHONE
            string userNotificationMessage = null; // TODO:  How to increase score?
            #else
            #error "Unrecognized platform."
            #endif

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 25, 0, 0),
                Children =
                {
                    new Label
                    {
                        Text = "Participation Level",
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    },
                    new Label
                    {
                        Text = Math.Round(protocol.Participation * 100, 0) + "%",
                        FontSize = 50,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    },                    
                    new Label
                    {                                
                        Text = "This score reflects your participation level over the past " + (protocol.ParticipationHorizonDays == 1 ? "day" : protocol.ParticipationHorizonDays + " days") + "." +
                        (participationRewardDatumId == null ? "" : " Anyone can verify your participation by scanning the following barcode from within Sensus:"),
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    }
                }
            };

            if (participationRewardDatumId != null)
                contentLayout.Children.Add(new Image
                    { 
                        Source = UiBoundSensusServiceHelper.Get(true).GetQrCodeImageSource(participationRewardDatumId),
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    });

            contentLayout.Children.Add(new Label
                {
                    Text = howToIncreaseScore,
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.CenterAndExpand
                });

            if (!string.IsNullOrWhiteSpace(protocol.ContactEmail))
            {
                Button emailStudyManagerButton = new Button
                {
                    Text = "Email Study Manager",
                    FontSize = 20
                };

                emailStudyManagerButton.Clicked += (o, e) =>
                {
                    UiBoundSensusServiceHelper.Get(true).SendEmailAsync(protocol.ContactEmail, "Help with Sensus study:  " + protocol.Name, 
                        "Hello - " + Environment.NewLine +
                        Environment.NewLine +
                        "I am having trouble with a Sensus study. The name of the study is \"" + protocol.Name + "\"." + Environment.NewLine +
                        Environment.NewLine +
                        "Here is why I am sending this email:  ");
                };

                contentLayout.Children.Add(emailStudyManagerButton);
            }

            Button viewParticipationDetailsButton = new Button
            {
                Text = "View Participation Details",
                FontSize = 20
            };

            viewParticipationDetailsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ParticipationReportDetailsPage(protocol));
            };

            contentLayout.Children.Add(viewParticipationDetailsButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}