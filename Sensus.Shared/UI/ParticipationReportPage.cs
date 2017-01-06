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
using System.Timers;
using Sensus.Context;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ParticipationReportPage : ContentPage
    {
        public ParticipationReportPage(Protocol protocol, ParticipationRewardDatum participationRewardDatum, bool displayDatumQrCode)
        {
            Title = protocol.Name;

#if __IOS__
            string howToIncreaseScore = "You can increase your score by opening Sensus more often and responding to questions that Sensus asks you.";
#elif __ANDROID__
            string howToIncreaseScore = "You can increase your score by allowing Sensus to run continuously and responding to questions that Sensus asks you.";
#elif WINDOWS_PHONE
            string userNotificationMessage = null; // TODO:  How to increase score?
#elif LOCAL_TESTS
            string howToIncreaseScore = null;
#else
#warning "Unrecognized platform."
            string howToIncreaseScore = "You can increase your score by opening Sensus more often and responding to questions that Sensus asks you.";
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
                        Text = Math.Round(participationRewardDatum.Participation * 100, 0) + "%",
                        FontSize = 50,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    },
                    new Label
                    {
                        Text = "This score reflects your participation level over the past " + (protocol.ParticipationHorizonDays == 1 ? "day" : protocol.ParticipationHorizonDays + " days") + "." +
                        (displayDatumQrCode ? " Anyone can verify your participation by tapping \"Scan Participation Barcode\" on their device and scanning the following barcode:" : ""),
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    }
                }
            };

            if (displayDatumQrCode)
            {
                Label expirationLabel = new Label
                {
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.CenterAndExpand
                };

                contentLayout.Children.Add(expirationLabel);

                Timer timer = new Timer(1000);

                timer.Elapsed += (o, e) =>
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        int secondsLeftBeforeBarcodeExpiration = (int)(SensusServiceHelper.PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS - (DateTimeOffset.UtcNow - participationRewardDatum.Timestamp).TotalSeconds);

                        if (secondsLeftBeforeBarcodeExpiration <= 0)
                        {
                            expirationLabel.TextColor = Color.Red;
                            expirationLabel.Text = "Barcode has expired. Please reopen this page to renew it.";
                            timer.Stop();
                        }
                        else
                        {
                            --secondsLeftBeforeBarcodeExpiration;
                            expirationLabel.Text = "Barcode will expire in " + secondsLeftBeforeBarcodeExpiration + " second" + (secondsLeftBeforeBarcodeExpiration == 1 ? "" : "s") + ".";
                        }
                    });
                };

                timer.Start();

                Disappearing += (o, e) =>
                {
                    timer.Stop();
                };

                contentLayout.Children.Add(new Image
                {
                    Source = SensusServiceHelper.Get().GetQrCodeImageSource(protocol.RemoteDataStore.GetDatumKey(participationRewardDatum)),
                    HorizontalOptions = LayoutOptions.CenterAndExpand
                });
            }

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
                    SensusServiceHelper.Get().SendEmailAsync(protocol.ContactEmail, "Help with Sensus study:  " + protocol.Name,
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