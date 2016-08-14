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

namespace SensusUI
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
                Text = "Your Studies",
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
                Text = "Pending Surveys",
                FontSize = 30,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            surveysButton.Clicked += async (sender, e) =>
            {
                await Navigation.PushAsync(new PendingScriptsPage());
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children =
                {
                    studiesButton,
                    surveysButton
                }
            };
        }
    }
}