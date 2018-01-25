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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class HomePage : ContentPage
    {
        public HomePage()
        {
            Title = "Home";

            Button studiesButton = new Button
            {
                BorderColor = Color.Accent,
                BorderWidth = 1,
                Text = "Manage My Studies",
                FontSize = 30,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            studiesButton.Clicked += async (sender, e) =>
            {
                await Navigation.PushAsync(new ProtocolsPage());
            };

            Button surveysButton = new Button
            {
                BorderColor = Color.Accent,
                BorderWidth = 1,
                Text = "Take Surveys",
                FontSize = 30,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            surveysButton.Clicked += async (sender, e) =>
            {
                await Navigation.PushAsync(new PendingScriptsPage());
            };

            Label privacyPolicyLabel = new Label
            {
                Text = "Privacy Policy",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            TapGestureRecognizer privacyPolicyTappedRecognizer = new TapGestureRecognizer();
            privacyPolicyTappedRecognizer.Tapped += async (s, e) =>
            {
                ContentPage privacyPolicyPage = new ContentPage
                {
                    Title = "Privacy Policy",
                    Content = new ScrollView
                    {
                        Content = new Label { Text = "Immediately following installation, Sensus will not collect, store, or upload any personal information from the device on which it is running. Sensus will upload reports when the app crashes. These reports contain information about the state of the app when it crashed, and Sensus developers will use these crash reports to improve Sensus. These reports do not contain any personal information. After you load a study into Sensus, Sensus will begin collecting data as defined by the study. You will be notified when the study is loaded and is about to start, and you will be asked to confirm that you wish to start the study. This confirmation will summarize the types of data to be collected. You may quit a study and/or uninstall Sensus at any time. Be aware that Sensus is publicly available and that anyone can use Sensus to design a study, which they can then share with others. Studies have the ability to collect personal information, and you should exercise caution when loading any study that you receive." }
                    }
                };

                await Navigation.PushAsync(privacyPolicyPage);
            };

            privacyPolicyLabel.GestureRecognizers.Add(privacyPolicyTappedRecognizer);

            ToolbarItems.Add(new ToolbarItem(null, "gear_wrench.png", async () =>
            {
                double shareDirectoryMB = SensusServiceHelper.GetDirectorySizeMB(SensusServiceHelper.SHARE_DIRECTORY);
                string clearShareDirectoryAction = "Clear Share Directory (" + Math.Round(shareDirectoryMB, 1) + " MB)";

                List<string> buttons = new string[] { "View Device ID", "View Log", "View Points of Interest", clearShareDirectoryAction }.ToList();

                // stopping only makes sense on android, where we use a background service. on ios, there is no concept
                // of stopping the app other than the user or system terminating the app.
#if __ANDROID__
                buttons.Add("Stop Sensus");
#endif

                buttons.Add("About Sensus");

                string action = await DisplayActionSheet("Actions", "Back", null, buttons.ToArray());

                if (action == "View Device ID")
                {
                    await DisplayAlert("Device ID", SensusServiceHelper.Get().DeviceId, "Close");
                }
                else if (action == "View Log")
                {
                    await Navigation.PushAsync(new ViewTextLinesPage("Log", SensusServiceHelper.Get().Logger.Read(200, true),
                        () =>
                        {
                            string sharePath = null;
                            try
                            {
                                sharePath = SensusServiceHelper.Get().GetSharePath(".txt");
                                SensusServiceHelper.Get().Logger.CopyTo(sharePath);
                            }
                            catch (Exception)
                            {
                                sharePath = null;
                            }

                            if (sharePath != null)
                            {
                                SensusServiceHelper.Get().ShareFileAsync(sharePath, "Log:  " + Path.GetFileName(sharePath), "text/plain");
                            }
                        },
                        () => SensusServiceHelper.Get().Logger.Clear()));
                }
                else if (action == "View Points of Interest")
                {
                    await Navigation.PushAsync(new PointsOfInterestPage(SensusServiceHelper.Get().PointsOfInterest));
                }
                else if (action == clearShareDirectoryAction)
                {
                    foreach (string sharePath in Directory.GetFiles(SensusServiceHelper.SHARE_DIRECTORY))
                    {
                        try
                        {
                            File.Delete(sharePath);
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = "Failed to delete shared file \"" + Path.GetFileName(sharePath) + "\":  " + ex.Message;
                            SensusServiceHelper.Get().FlashNotificationAsync(errorMessage);
                            SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                        }
                    }
                }
#if __ANDROID__
                else if (action == "Stop Sensus" && await DisplayAlert("Confirm", "Are you sure you want to stop Sensus? This will end your participation in all studies.", "Stop Sensus", "Go Back"))
                {
                    SensusServiceHelper.Get().StopProtocols();
                    (SensusServiceHelper.Get() as Android.IAndroidSensusServiceHelper)?.StopAndroidSensusService();
                }
#endif
                else if (action == "About Sensus")
                {
                    await DisplayAlert("About Sensus", "Version:  " + SensusServiceHelper.Get().Version, "OK");
                }
            }));

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children =
                {
                    studiesButton,
                    surveysButton,
                    privacyPolicyLabel,
                    new Label()
                }
            };
        }
    }
}