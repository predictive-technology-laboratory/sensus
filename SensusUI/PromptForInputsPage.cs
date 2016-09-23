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
using SensusService;

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

        private bool _canNavigateBack;
        private Action<Result> _finishedCallback;
        private int _displayedInputCount;

        public int DisplayedInputCount
        {
            get
            {
                return _displayedInputCount;
            }
        }

        public PromptForInputsPage(InputGroup inputGroup,
                                   int stepNumber,
                                   int totalSteps,
                                   bool canNavigateBack,
                                   bool showCancelButton,
                                   string nextButtonTextOverride,
                                   CancellationToken? cancellationToken,
                                   string cancelConfirmation,
                                   string incompleteSubmissionConfirmation,
                                   string submitConfirmation,
                                   bool displayProgress,
                                   DateTimeOffset? firstPromptTimestamp,
                                   Action<Result> finishedCallback)
        {
            _canNavigateBack = canNavigateBack;
            _finishedCallback = finishedCallback;
            _displayedInputCount = 0;

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(10, 20, 10, 20),
                Children =
                {
                    new Label
                    {
                        Text = inputGroup.Name,
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    }
                }
            };

            if (displayProgress)
            {
                float progress = (stepNumber - 1) / (float)totalSteps;

                contentLayout.Children.Add(new Label
                {
                    Text = "Progress:  " + Math.Round(100 * progress) + "%",
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.CenterAndExpand
                });

                contentLayout.Children.Add(new ProgressBar
                {
                    Progress = progress,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                });
            }

            // for prompts that have been shown before, display the original timestamp.
            if (firstPromptTimestamp.HasValue)
            {
                DateTime firstDisplayDateTime = firstPromptTimestamp.Value.ToLocalTime().DateTime;

                string displayLapseDayDesc;
                if (firstDisplayDateTime.Date == DateTime.Now.Date)
                    displayLapseDayDesc = "earlier today";
                else if (firstDisplayDateTime.Date == DateTime.Now.AddDays(-1).Date)
                    displayLapseDayDesc = "yesterday";
                else
                    displayLapseDayDesc = ((int)(DateTime.Now - firstDisplayDateTime).TotalDays) + " days ago (" + firstDisplayDateTime.ToShortDateString() + ")";

                contentLayout.Children.Add(new Label
                {
                    Text = "This form was created " + displayLapseDayDesc + " at " + firstDisplayDateTime.ToShortTimeString() + ".",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.Start
                });
            }

            // indicate required fields
            if (inputGroup.Inputs.Any(input => input.Display && input.Required))
                contentLayout.Children.Add(new Label
                {
                    Text = "Required fields are indicated with *",
                    FontSize = 15,
                    TextColor = Color.Red,
                    HorizontalOptions = LayoutOptions.Start
                });

            // add inputs to the page
            List<Input> displayedInputs = new List<Input>();
            int viewNumber = 1;
            int inputSeparatorHeight = 10;
            foreach (Input input in inputGroup.Inputs)
                if (input.Display)
                {
                    View inputView = input.GetView(viewNumber);
                    if (inputView != null)
                    {
                        // frame all enabled inputs that request a frame
                        if (input.Enabled && input.Frame)
                        {
                            inputView = new Frame
                            {
                                Content = inputView,
                                OutlineColor = Color.Accent,
                                VerticalOptions = LayoutOptions.Start,
                                HasShadow = true,
                                Padding = new Thickness(10)
                            };
                        }

                        // add some vertical separation between inputs
                        if (_displayedInputCount > 0)
                            contentLayout.Children.Add(new BoxView { Color = Color.Transparent, HeightRequest = inputSeparatorHeight });

                        contentLayout.Children.Add(inputView);
                        displayedInputs.Add(input);

                        if (input.DisplayNumber)
                            ++viewNumber;

                        ++_displayedInputCount;
                    }
                }

            // add final separator if we displayed any inputs
            if (_displayedInputCount > 0)
                contentLayout.Children.Add(new BoxView { Color = Color.Transparent, HeightRequest = inputSeparatorHeight });

            StackLayout navigationStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };

            #region previous/next buttons
            StackLayout previousNextStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            // add a prevous button if we're allowed to navigate back
            if (_canNavigateBack)
            {
                Button previousButton = new Button
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20,
                    Text = "Previous"
                };

                previousButton.Clicked += (o, e) =>
                {
                    _finishedCallback(Result.NavigateBackward);
                };

                previousNextStack.Children.Add(previousButton);
            }

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

            nextButton.Clicked += async (o, e) =>
            {
                string confirmationMessage = "";

                if (!string.IsNullOrWhiteSpace(incompleteSubmissionConfirmation) && !inputGroup.Valid)
                    confirmationMessage += incompleteSubmissionConfirmation;
                else if (nextButton.Text == "Submit" && !string.IsNullOrWhiteSpace(submitConfirmation))
                    confirmationMessage += submitConfirmation;

                if (string.IsNullOrWhiteSpace(confirmationMessage) || await DisplayAlert("Confirm", confirmationMessage, "Yes", "No"))
                    _finishedCallback(Result.NavigateForward);
            };

            previousNextStack.Children.Add(nextButton);
            navigationStack.Children.Add(previousNextStack);
            #endregion

            #region cancel button and token
            if (showCancelButton)
            {
                Button cancelButton = new Button
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20,
                    Text = "Cancel"
                };

                // separate cancel button from previous/next with a thin visible separator
                navigationStack.Children.Add(new BoxView { Color = Color.Gray, HorizontalOptions = LayoutOptions.FillAndExpand, HeightRequest = 0.5 });
                navigationStack.Children.Add(cancelButton);

                cancelButton.Clicked += async (o, e) =>
                {
                    if (string.IsNullOrWhiteSpace(cancelConfirmation) || await DisplayAlert("Confirm", cancelConfirmation, "Yes", "No"))
                        _finishedCallback(Result.Cancel);
                };
            }

            contentLayout.Children.Add(navigationStack);

            if (cancellationToken.HasValue)
            {
                cancellationToken.Value.Register(() =>
                {
                    // it is possible for the token to be canceled from a thread other than the UI thread. the finished callback will do 
                    // things with the UI, so ensure that the finished callback is run on the UI thread.
                    SensusServiceHelper.Get().MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        SensusServiceHelper.Get().Logger.Log("Cancellation token has been cancelled.", LoggingLevel.Normal, GetType());
                        _finishedCallback(Result.Cancel);
                    });
                });
            }
            #endregion

            Appearing += (o, e) =>
            {
                // the page has appeared so mark all inputs as viewed
                foreach (Input input in displayedInputs)
                    input.Viewed = true;
            };

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }

        /// <summary>
        /// Disable the device's back button. The user must complete the form.
        /// </summary>
        /// <returns>True</returns>
        protected override bool OnBackButtonPressed()
        {
            if (_canNavigateBack)
                _finishedCallback(Result.NavigateBackward);

            return true;
        }
    }
}