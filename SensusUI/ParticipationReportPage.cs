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
        public ParticipationReportPage(Protocol protocol)
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

            Button helpButton = null;
            if (!string.IsNullOrWhiteSpace(protocol.ContactEmail))
            {
                helpButton = new Button
                {
                    Text = "Email Study Manager",
                    FontSize = 20
                };

                helpButton.Clicked += (o, e) =>
                {
                    UiBoundSensusServiceHelper.Get(true).SendEmailAsync(protocol.ContactEmail, "Help with study:  " + protocol.Name, 
                        "Hello - " + Environment.NewLine +
                        Environment.NewLine +
                        "I am having trouble with a Sensus study. The name of the study is \"" + protocol.Name + "\"." + Environment.NewLine +
                        Environment.NewLine +
                        "[What is Sensus doing that caused you to send this email]:  " + Environment.NewLine +
                        Environment.NewLine +
                        "[What other concerns do you have about this study]:  ");
                };
            }

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 50, 0, 0),
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
                        FontSize = 75,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    },
                    new Label
                    {                                
                        Text = "This score reflects your overall participation level in the \"" + protocol.Name + "\" study over the past " + (protocol.ParticipationHorizonDays == 1 ? "day" : protocol.ParticipationHorizonDays + " days") + ". " + howToIncreaseScore + (helpButton == null ? "" : Environment.NewLine + Environment.NewLine + "If you have questions, please click the button below to email the study manager."),
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    }
                }
            };

            if (helpButton != null)
                contentLayout.Children.Add(helpButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}