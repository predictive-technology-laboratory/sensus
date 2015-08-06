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
using System.Collections.Generic;
using SensusService.Exceptions;
using System.Linq;
using SensusUI.Inputs;
using System.Threading;

namespace SensusUI
{
    public class PromptForInputsPage : ContentPage
    {
        public PromptForInputsPage(InputGroup inputGroup, int stepNumber, int totalSteps)
        {
            Title = inputGroup.Name;

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 10, 0, 0)
            };

            contentLayout.Children.Add(new Label
                {
                    Text = "Step " + stepNumber + " of " + totalSteps,
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.CenterAndExpand
                });

            contentLayout.Children.Add(new ProgressBar
                {
                    Progress = stepNumber / (double)totalSteps,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                });

            foreach (Input input in inputGroup.Inputs)
                contentLayout.Children.Add(input.View);

            Button cancelButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = "Cancel"
            };

            cancelButton.Clicked += async (o, e) =>
            {
                await Navigation.PopAsync();
            };

            Button okButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = "OK"
            };

            okButton.Clicked += async (o, e) =>
            {
                await Navigation.PopAsync();
            };
                
            contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { cancelButton, okButton }
                });

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}