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
                    Source = SensusServiceHelper.Get().GetQrCodeImageSource(QrCodePrefix.SENSUS_PARTICIPATION + protocol.RemoteDataStore.GetDatumKey(participationRewardDatum)),
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
                    Text = "Email Study Manager for Help",
                    FontSize = 20
                };

                emailStudyManagerButton.Clicked += async (o, e) =>
                {
                    await SensusServiceHelper.Get().SendEmailAsync(protocol.ContactEmail, "Help with Sensus study:  " + protocol.Name,
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
