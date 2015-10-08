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

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 20, 0, 0)
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

            #region set up buttons

            Button nextButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Scale = 0.9,
                BorderColor = Color.White,
                BackgroundColor = Color.Transparent,
                BorderWidth = 1,
                Text = stepNumber < totalSteps ? "Next" : "Submit"
            };

            bool nextButtonTapped = false;

            Button skipButton = new Button
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 19,
                    Scale = 0.6,
                    BackgroundColor = Color.Transparent,
                    TextColor = Color.Silver,
                    Text = "Skip question"
                };

            bool skipButtonTapped = false;

            #endregion

            #region set up button click events

            nextButton.Clicked += async (o, e) =>
                {
                    bool complete = true;
                    bool skip = false;
                    foreach (Input input in inputGroup.Inputs)
                    {
                        if (!(input.Complete) && !(input is TextInput || input is LabelOnlyInput) && totalSteps > 1)
                        {
                            if (totalSteps == stepNumber)
                            {
                                skip = await DisplayAlert("Skip question?", "Please only skip if a) you do not feel comfortable answering this question, or b) per the question statement, this question does not apply to you.", "Yes", "No");
                                if (!skip)
                                    complete = false;
                                else
                                    complete = true;
                            }
                            else
                            {
                                complete = false;
                                await DisplayAlert("Incomplete", "Please complete this step before moving on.", "Ok");
                                break;
                            }
                        }
                    }
                    if (complete)
                    {
                        nextButtonTapped = true;
                        foreach (Input input2 in inputGroup.Inputs)
                            input2.Complete = true;
                        await Navigation.PopModalAsync();
                    }
                };

            skipButton.Clicked += async (o, e) =>
                {
                    skipButtonTapped = await DisplayAlert("Skip question?", "Please only skip if a) you do not feel comfortable answering this question, or b) per the question statement, this question does not apply to you.", "Yes", "No");
                    if (skipButtonTapped)
                    {
                        foreach (Input input in inputGroup.Inputs)
                            input.Complete = true;
                        await Navigation.PopModalAsync();
                    }
                };

            #endregion

            if (totalSteps == 1 || stepNumber == totalSteps)
                contentLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.EndAndExpand,
                        Children = { nextButton }
                    });
            else
                contentLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.EndAndExpand,
                        Children = { nextButton, skipButton }
                    });

            Disappearing +=(o, e) =>
            {
                if (nextButtonTapped || skipButtonTapped)
                    callback(Result.NavigateForward);
                else
                    callback(Result.Cancel);  // the user navigated back, or another activity started
            };

            Content = new ScrollView
            {
                Content = contentLayout,
                Padding = new Thickness(10, 10, 10, 10),
            };

            NavigationPage.SetHasNavigationBar(this, true);
        }
    }
}