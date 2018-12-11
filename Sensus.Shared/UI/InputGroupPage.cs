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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Sensus.UI.Inputs;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace Sensus.UI
{
    public class InputGroupPage : ContentPage
    {
        public enum NavigationResult
        {
            Backward,
            Forward,
            Submit,
            Cancel
        }

        private bool _canNavigateBackward;
        private int _displayedInputCount;
        private TaskCompletionSource<NavigationResult> _responseTaskCompletionSource;

        public int DisplayedInputCount
        {
            get
            {
                return _displayedInputCount;
            }
        }

        public Task<NavigationResult> ResponseTask
        {
            get { return _responseTaskCompletionSource.Task; }
        }

        public InputGroupPage(InputGroup inputGroup,
                              int stepNumber,
                              int totalSteps,
                              bool canNavigateBackward,
                              bool showCancelButton,
                              string nextButtonTextOverride,
                              CancellationToken? cancellationToken,
                              string cancelConfirmation,
                              string incompleteSubmissionConfirmation,
                              string submitConfirmation,
                              bool displayProgress)
        {
            _canNavigateBackward = canNavigateBackward;
            _displayedInputCount = 0;
            _responseTaskCompletionSource = new TaskCompletionSource<NavigationResult>();

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

            #region progress bar
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
            #endregion

            #region required field label
            if (inputGroup.Inputs.Any(input => input.Display && input.Required))
            {
                contentLayout.Children.Add(new Label
                {
                    Text = "Required fields are indicated with *",
                    FontSize = 15,
                    TextColor = Color.Red,
                    HorizontalOptions = LayoutOptions.Start
                });
            }
            #endregion

            #region inputs
            List<Input> displayedInputs = new List<Input>();
            int viewNumber = 1;
            int inputSeparatorHeight = 10;
            foreach (Input input in inputGroup.Inputs)
            {
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
                                BorderColor = Color.Accent,
                                BackgroundColor = Color.Transparent,
                                VerticalOptions = LayoutOptions.Start,
                                HasShadow = false,
                                Padding = new Thickness(10)
                            };
                        }

                        // add some vertical separation between inputs
                        if (_displayedInputCount > 0)
                        {
                            contentLayout.Children.Add(new BoxView { Color = Color.Transparent, HeightRequest = inputSeparatorHeight });
                        }

                        contentLayout.Children.Add(inputView);
                        displayedInputs.Add(input);

                        if (input.DisplayNumber)
                        {
                            viewNumber++;
                        }

                        _displayedInputCount++;
                    }
                }
            }

            // add final separator if we displayed any inputs
            if (_displayedInputCount > 0)
            {
                contentLayout.Children.Add(new BoxView { Color = Color.Transparent, HeightRequest = inputSeparatorHeight });
            }
            #endregion

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
            if (_canNavigateBackward)
            {
                Button previousButton = new Button
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20,
                    Text = "Previous"
                };

                previousButton.Clicked += (o, e) =>
                {
                    _responseTaskCompletionSource.TrySetResult(NavigationResult.Backward);
                };

                previousNextStack.Children.Add(previousButton);
            }

            Button nextButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = stepNumber < totalSteps ? "Next" : "Submit"

#if UI_TESTING
                // set style id so that we can retrieve the button when UI testing
                , StyleId = "NextButton"
#endif
            };

            if (nextButtonTextOverride != null)
            {
                nextButton.Text = nextButtonTextOverride;
            }

            nextButton.Clicked += async (o, e) =>
            {
                if (!inputGroup.Valid && inputGroup.ForceValidInputs)
                {
                    await DisplayAlert("Mandatory", "You must provide values for all required fields before proceeding.", "Back");
                }
                else
                {
                    string confirmationMessage = "";
                    NavigationResult navigationResult = NavigationResult.Forward;

                    // warn about incomplete inputs if a message is provided
                    if (!inputGroup.Valid && !string.IsNullOrWhiteSpace(incompleteSubmissionConfirmation))
                    {
                        confirmationMessage += incompleteSubmissionConfirmation;
                    }

                    if (nextButton.Text == "Submit")
                    {
                        // confirm submission if a message is provided
                        if (!string.IsNullOrWhiteSpace(submitConfirmation))
                        {
                            // if we already warned about incomplete fields, make the submit confirmation sound natural.
                            if (!string.IsNullOrWhiteSpace(confirmationMessage))
                            {
                                confirmationMessage += " Also, this is the final page. ";
                            }

                            // confirm submission
                            confirmationMessage += submitConfirmation;
                        }

                        navigationResult = NavigationResult.Submit;
                    }

                    if (string.IsNullOrWhiteSpace(confirmationMessage) || await DisplayAlert("Confirm", confirmationMessage, "Yes", "No"))
                    {
                        _responseTaskCompletionSource.TrySetResult(navigationResult);
                    }
                }
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
                    {
                        _responseTaskCompletionSource.TrySetResult(NavigationResult.Cancel);
                    }
                };
            }

            contentLayout.Children.Add(navigationStack);

            // allow the cancellation token to set the result of this page
            cancellationToken?.Register(() =>
            {
                _responseTaskCompletionSource.TrySetResult(NavigationResult.Cancel);
            });
            #endregion

            Appearing += (o, e) =>
            {
                // the page has appeared so mark all inputs as viewed
                foreach (Input displayedInput in displayedInputs)
                {
                    displayedInput.Viewed = true;
                }
            };

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }

        protected override bool OnBackButtonPressed()
        {
            // the only applies to phones with a hard/soft back button. iOS does not have this button. on 
            // android, allow the user to cancel the page with the back button.
            _responseTaskCompletionSource.TrySetResult(NavigationResult.Cancel);

            return base.OnBackButtonPressed();
        }
    }
}
