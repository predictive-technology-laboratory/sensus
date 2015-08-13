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
using Android.App;
using Android.Runtime;
using Android.Support.V7.AppCompat;

namespace SensusUI
{
    public class PromptForInputsPage : ContentPage
    {
        private int _steps;

        public enum Result
        {
            NavigateBackward,
            NavigateForward,
            Cancel
        }

        public PromptForInputsPage(InputGroup inputGroup, int stepNumber, int totalSteps, Action<Result> callback)
        {
            if (stepNumber == 1 && totalSteps != 1)
                NavigationPage.SetHasBackButton(this, false);
            else
                NavigationPage.SetHasBackButton(this, true);

            Title = inputGroup.Name;

            _steps = totalSteps;

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
                if (input.View != null)
                    contentLayout.Children.Add(input.View);

            Button nextButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = stepNumber < totalSteps ? "Next" : "Submit"
            };

            bool nextButtonTapped = false;

            nextButton.Clicked += async (o, e) =>
            {
                    bool complete = true;
                    foreach (Input input in inputGroup.Inputs)
                    {
                        if (!(input is TextInput) && !(input.Complete) && totalSteps > 1)
                        {
                            complete = false;
                            await DisplayAlert("Alert", "You must complete this step before moving on.", "Ok");
                            break;
                        }
                    }
                    if (complete)
                    {
                        nextButtonTapped = true;
                        await Navigation.PopAsync();
                    }
            };
                
            contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.EndAndExpand,
                    Children = { nextButton }
                });

            Disappearing += async (o, e) =>
            {
                if (nextButtonTapped)
                    callback(Result.NavigateForward);
                else
                    callback(Result.NavigateBackward);
            };

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}