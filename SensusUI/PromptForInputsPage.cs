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
        public enum Result
        {
            NavigateBackward,
            NavigateForward,
            Cancel
        }

        public PromptForInputsPage(InputGroup inputGroup, int stepNumber, int totalSteps, bool showCancelButton, string nextButtonTextOverride, CancellationToken? cancellationToken, Action<Result> callback)
        {            
            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 20, 0, 0),
                Children =
                {
                    new Label
                    {
                        Text = inputGroup.Name,
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    },
                    new Label
                    {
                        Text = "Step " + stepNumber + " of " + totalSteps,
                        FontSize = 15,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    },
                    new ProgressBar
                    {
                        Progress = stepNumber / (double)totalSteps,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    }
                }
            };

            int viewNumber = 1;
            foreach (Input input in inputGroup.Inputs)
            {
                View inputView = input.GetView(viewNumber);
                if (inputView != null)
                {
                    contentLayout.Children.Add(inputView);

                    if (!(input is LabelOnlyInput))
                        ++viewNumber;
                }
            }

            StackLayout navigationStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            #region previous button

            bool previousButtonTapped = false;

            // step numbers are 1-based -- if we're beyond the first, provide a previous button
            if (stepNumber > 1)
            {
                Button previousButton = new Button
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20,
                    Text = "Previous"
                };

                navigationStack.Children.Add(previousButton);

                previousButton.Clicked += async (o, e) =>
                {
                    previousButtonTapped = true;
                    await Navigation.PopModalAsync(false);
                };                      
            }

            #endregion

            #region cancel button

            bool cancelButtonTapped = false;

            if (showCancelButton)
            {
                Button cancelButton = new Button
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20,
                    Text = "Cancel"
                };

                navigationStack.Children.Add(cancelButton);

                cancelButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Confirm", "Would you like to cancel?", "Yes", "No"))
                    {
                        cancelButtonTapped = true;
                        await Navigation.PopModalAsync(true);
                    }
                };
            }

            #endregion

            #region next button

            Button nextButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = stepNumber < totalSteps ? "Next" : "Submit"

                #if UNIT_TESTING
                // set style id so that we can retrieve the button when unit testing
                , StyleId = "NextButton"
                #endif
            };

            if (nextButtonTextOverride != null)
                nextButton.Text = nextButtonTextOverride;

            navigationStack.Children.Add(nextButton);

            bool nextButtonTapped = false;

            nextButton.Clicked += async (o, e) =>
            {
                if (nextButton.Text == "Next" || await DisplayAlert("Confirm", "Would you like to proceed?", "Yes", "No"))
                {
                    nextButtonTapped = true;
                    await Navigation.PopModalAsync(stepNumber == totalSteps);
                }
            };

            #endregion
                
            contentLayout.Children.Add(navigationStack);

            #region cancellation token

            bool cancellationTokenCanceled = false;

            if (cancellationToken != null)
            {
                cancellationToken.GetValueOrDefault().Register(() =>
                    {                        
                        cancellationTokenCanceled = true;

                        Device.BeginInvokeOnMainThread(async() =>
                            {
                                if (Navigation.ModalStack.Count > 0 && Navigation.ModalStack.Last() == this)
                                    await Navigation.PopModalAsync(true);
                            });
                    });
            }

            #endregion
            
            Disappearing += (o, e) =>
            {
                if (previousButtonTapped)
                    callback(Result.NavigateBackward);
                else if (cancelButtonTapped || cancellationTokenCanceled)
                    callback(Result.Cancel);
                else if (nextButtonTapped)
                    callback(Result.NavigateForward);
                else
                    callback(Result.Cancel);  // the user navigated back, or another activity started
            };                    

            Content = new ScrollView
            {
                Content = contentLayout
            };                        
        }
    }
}