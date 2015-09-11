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
using SensusUI.Inputs;
using SensusUI.UiProperties;

namespace SensusUI
{
    public class ScriptInputGroupPage : ContentPage
    {
        public ScriptInputGroupPage(InputGroup inputGroup)
        {
            Title = "Input Group";

            StackLayout contentLayout = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(inputGroup))
                contentLayout.Children.Add(stack);

            Button editInputsButton = new Button
            {
                Text = "Edit Inputs",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            editInputsButton.Clicked += async (o, e) =>
            {
                await Navigation.PushAsync(new ScriptInputsPage(inputGroup));
            };

            contentLayout.Children.Add(editInputsButton);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}