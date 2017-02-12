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
                HorizontalOptions = LayoutOptions.Center
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

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children =
                {
                    studiesButton,
                    surveysButton,
                    privacyPolicyLabel
                }
            };
        }
    }
}