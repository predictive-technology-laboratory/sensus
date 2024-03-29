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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Sensus.UI.Inputs;
using Xamarin.Forms;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using Sensus.Context;

namespace Sensus.UI
{
	public class InputGroupPage : ContentPage
	{
		public enum NavigationResult
		{
			None,
			Backward,
			Forward,
			Submit,
			Cancel,
			Timeout,
			Paused
		}

		private InputGroup _inputGroup;
		private StackLayout _navigationStack;
		private bool _canNavigateBackward;
		private List<Input> _displayedInputs;
		private int _displayedInputCount;
		private TaskCompletionSource<NavigationResult> _responseTaskCompletionSource;
		private ShowNavigationOptions _showNavigationButtons;
		private bool _confirmNavigation;
		private readonly string _incompleteSubmissionConfirmation;
		private readonly string _submitConfirmation;
		private Timer _timer;
		private bool _savedState;

		public int DisplayedInputCount
		{
			get
			{
				return _displayedInputCount;
			}
		}

		protected Task<NavigationResult> ResponseTask
		{
			get { return _responseTaskCompletionSource.Task; }
		}

		public bool IsLastPage { get; }

		public string InputGroupId => _inputGroup.Id;

		public Page ReturnPage { get; set; }

		public InputGroupPage(InputGroup inputGroup,
							  int stepNumber,
							  int totalSteps,
							  bool canNavigateBackward,
							  bool showCancelButton,
							  string nextButtonTextOverride,
							  CancellationToken? cancellationToken,
							  bool confirmNavigation,
							  string cancelConfirmation,
							  string incompleteSubmissionConfirmation,
							  string submitConfirmation,
							  bool displayProgress,
							  string title = "",
							  bool savedState = false)
		{

			_inputGroup = inputGroup;
			_canNavigateBackward = canNavigateBackward && (inputGroup.HidePreviousButton == false);
			_displayedInputCount = 0;
			_responseTaskCompletionSource = new TaskCompletionSource<NavigationResult>();
			_showNavigationButtons = inputGroup.ShowNavigationButtons;
			_confirmNavigation = confirmNavigation;
			_incompleteSubmissionConfirmation = incompleteSubmissionConfirmation;
			_submitConfirmation = submitConfirmation;
			_savedState = savedState;

			IsLastPage = totalSteps <= stepNumber;
			Title = inputGroup.Title ?? title;

			StackLayout contentLayout = new StackLayout
			{
				Orientation = StackOrientation.Vertical,
				VerticalOptions = LayoutOptions.FillAndExpand,
				Padding = new Thickness(10, 10, 10, 20)
			};

			ScrollView scrollView = new ScrollView
			{
				Content = contentLayout
			};

			//bool scrolled = false;

			//scrollView.SizeChanged += async (s, e) =>
			//{
			//	if (scrolled == false)
			//	{
			//		await scrollView.ScrollToAsync(0, 0, false);

			//		scrolled = true;
			//	}
			//};

			StackLayout headerLayout = new StackLayout
			{
				Orientation = StackOrientation.Vertical,
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			StackLayout subHeaderLayout = new StackLayout
			{
				Orientation = StackOrientation.Vertical,
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			if (inputGroup.UseNavigationBar == false)
			{
				if (string.IsNullOrWhiteSpace(inputGroup.Title) == false)
				{
					headerLayout.Children.Add(new Label
					{
						Text = inputGroup.Title,
						FontSize = 20,
						HorizontalOptions = LayoutOptions.CenterAndExpand
					});
				}
				else
				{
					headerLayout.Children.Add(new Label
					{
						Text = title,
						FontSize = 20,
						HorizontalOptions = LayoutOptions.CenterAndExpand
					});
				}
			}

			#region progress bar
			if (displayProgress && inputGroup.HideProgress == false)
			{
				float progress = (stepNumber - 1) / (float)totalSteps;

				subHeaderLayout.Children.Add(new Label
				{
					Text = "Progress: " + Math.Round(100 * progress) + "%",
					FontSize = 15,
					HorizontalOptions = LayoutOptions.CenterAndExpand
				});

				subHeaderLayout.Children.Add(new ProgressBar
				{
					Progress = progress,
					HorizontalOptions = LayoutOptions.FillAndExpand
				});
			}
			#endregion

			#region required field label
			if (inputGroup.HideRequiredFieldLabel == false && inputGroup.Inputs.Any(input => input.Display && input.Required))
			{
				subHeaderLayout.Children.Add(new Label
				{
					Text = "Required fields are indicated with *",
					FontSize = 15,
					StyleClass = new[] { "RequiredFieldLabel" },
					HorizontalOptions = LayoutOptions.Start
				});
			}
			#endregion

			if (subHeaderLayout.Children.Count > 0)
			{
				headerLayout.Children.Add(subHeaderLayout);
			}

			if (inputGroup.FreezeHeader && headerLayout.Children.Count > 0)
			{
				headerLayout.Padding = new Thickness(10 + scrollView.Margin.Left, 20, 10 + scrollView.Margin.Right, 0);

				Content = new StackLayout()
				{
					Orientation = StackOrientation.Vertical,
					Children = { headerLayout, scrollView }
				};
			}
			else
			{
				if (headerLayout.Children.Count > 0)
				{
					contentLayout.Children.Insert(0, headerLayout);
				}

				Content = scrollView;
			}

			_cancelHandler = async (o, e) =>
			{
				if (_confirmNavigation == false || string.IsNullOrWhiteSpace(cancelConfirmation) || await ConfirmNavigationAsync(cancelConfirmation))
				{
					Navigate(NavigationResult.Cancel);
				}
			};

			_previousHandler = (o, e) =>
			{
				if (_canNavigateBackward)
				{
					Navigate(NavigationResult.Backward);
				}
			};

			_nextHandler = async (o, e) =>
			{
				if (!_inputGroup.Valid && _inputGroup.ForceValidInputs)
				{
					await DisplayAlert("Mandatory", "You must provide values for all required fields before proceeding.", "Back");
				}
				else
				{
					if (await ConfirmForwardNavigationAsync())
					{
						if (IsLastPage)
						{
							Navigate(NavigationResult.Submit);
						}
						else
						{
							Navigate(NavigationResult.Forward);
						}
					}
				}
			};

			#region inputs
			List<Input> displayedInputs = new List<Input>();
			int viewNumber = 1;
			int inputSeparatorHeight = 10;
			foreach (Input input in inputGroup.Inputs)
			{
				input.InputGroupPage = this;
				input.ScriptRunner = inputGroup.ScriptRunner;

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
								StyleClass = new List<string> { "InputFrame" },
								Content = inputView,
								VerticalOptions = LayoutOptions.Start,
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

			_displayedInputs = displayedInputs;

			// add final separator if we displayed any inputs
			if (_displayedInputCount > 0)
			{
				contentLayout.Children.Add(new BoxView { Color = Color.Transparent, HeightRequest = inputSeparatorHeight });
			}
			#endregion

			if (inputGroup.ShowNavigationButtons != ShowNavigationOptions.Never)
			{
				_navigationStack = new StackLayout
				{
					Orientation = StackOrientation.Vertical,
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				if (inputGroup.ShowNavigationButtons != ShowNavigationOptions.Always)
				{
					_navigationStack.IsVisible = false;
				}

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
						StyleClass = new List<string> { "NavigationButton" },
						HorizontalOptions = LayoutOptions.FillAndExpand,
						FontSize = 20,
						Text = "Previous"
					};

					if (string.IsNullOrWhiteSpace(inputGroup.PreviousButtonText) == false)
					{
						previousButton.Text = inputGroup.PreviousButtonText;
					}

					previousButton.Clicked += _previousHandler;

					previousNextStack.Children.Add(previousButton);
				}

				Button nextButton = new Button
				{
					StyleClass = new List<string> { "NavigationButton" },
					HorizontalOptions = LayoutOptions.FillAndExpand,
					FontSize = 20,
					Text = "Next"

#if UI_TESTING
				// set style id so that we can retrieve the button when UI testing
				, StyleId = "NextButton"
#endif
				};

				if (string.IsNullOrWhiteSpace(nextButtonTextOverride) == false)
				{
					nextButton.Text = nextButtonTextOverride;
				}
				else if (IsLastPage)
				{
					if (string.IsNullOrWhiteSpace(inputGroup.SubmitButtonText) == false)
					{
						nextButton.Text = inputGroup.SubmitButtonText;
					}
					else
					{
						nextButton.Text = "Submit";
					}
				}
				else if (string.IsNullOrWhiteSpace(inputGroup.NextButtonText) == false)
				{
					nextButton.Text = inputGroup.NextButtonText;
				}

				nextButton.Clicked += _nextHandler;

				previousNextStack.Children.Add(nextButton);
				_navigationStack.Children.Add(previousNextStack);
				#endregion

				#region cancel button and token
				if (showCancelButton)
				{
					Button cancelButton = new Button
					{
						StyleClass = new List<string> { "NavigationButton" },
						HorizontalOptions = LayoutOptions.FillAndExpand,
						FontSize = 20,
						Text = "Cancel"
					};

					if (string.IsNullOrWhiteSpace(inputGroup.CancelButtonText) == false)
					{
						cancelButton.Text = inputGroup.CancelButtonText;
					}

					// separate cancel button from previous/next with a thin visible separator
					_navigationStack.Children.Add(new BoxView { Color = Color.Gray, HorizontalOptions = LayoutOptions.FillAndExpand, HeightRequest = 0.5 });
					_navigationStack.Children.Add(cancelButton);

					cancelButton.Clicked += _cancelHandler;
				}

				if (inputGroup.NavigationPlacement != NavigationButtonLocations.Outside)
				{
					if (inputGroup.NavigationPlacement == NavigationButtonLocations.End)
					{
						_navigationStack.VerticalOptions = LayoutOptions.EndAndExpand;
					}

					contentLayout.Children.Add(_navigationStack);
				}
				else
				{
					_navigationStack.Padding = contentLayout.Padding;

					if (Content is ScrollView contentScrollView)
					{
						Content = new StackLayout
						{
							Orientation = StackOrientation.Vertical,
							Children = { Content, _navigationStack }
						};
					}
					else if (Content is StackLayout contentStackLayout)
					{
						contentStackLayout.Children.Add(_navigationStack);
					}
				}

				// allow the cancellation token to set the result of this page
				cancellationToken?.Register(() =>
				{
					Interrupt(false, true);
				});
				#endregion
			}

			Appearing += (o, e) =>
			{
				// the page has appeared so mark all inputs as viewed
				foreach (Input displayedInput in displayedInputs)
				{
					displayedInput.Viewed = true;
				}
			};
		}

		protected EventHandler _cancelHandler;
		protected EventHandler _nextHandler;
		protected EventHandler _previousHandler;

		protected Task<bool> ConfirmNavigationAsync(string confirmationMessage)
		{
			return Application.Current.MainPage.DisplayAlert("Confirm", confirmationMessage, "Yes", "No");
		}
		public async Task<bool> ConfirmForwardNavigationAsync()
		{
			string confirmationMessage = "";

			// warn about incomplete inputs if a message is provided
			if (!_inputGroup.Valid && !string.IsNullOrWhiteSpace(_incompleteSubmissionConfirmation))
			{
				confirmationMessage += _incompleteSubmissionConfirmation;
			}

			if (IsLastPage)
			{
				// confirm submission if a message is provided
				if (!string.IsNullOrWhiteSpace(_submitConfirmation))
				{
					// if we already warned about incomplete fields, make the submit confirmation sound natural.
					if (!string.IsNullOrWhiteSpace(confirmationMessage))
					{
						confirmationMessage += " Also, this is the final page. ";
					}

					// confirm submission
					confirmationMessage += _submitConfirmation;
				}
			}

			if (_confirmNavigation && string.IsNullOrWhiteSpace(confirmationMessage) == false)
			{
				return await ConfirmNavigationAsync(confirmationMessage);
			}

			return true;
		}

		public async Task<NavigationResult> WaitForNavigationAsync()
		{
			return await _responseTaskCompletionSource.Task;
		}

		public void Navigate(NavigationResult navigationResult)
		{
			HandleStaleNavigation();

			if (navigationResult != NavigationResult.None)
			{
				_responseTaskCompletionSource.TrySetResult(navigationResult);
			}
		}

		public void Navigate(Input input, NavigationResult navigationResult)
		{
			if (navigationResult == NavigationResult.Cancel)
			{
				_cancelHandler?.Invoke(input, EventArgs.Empty);
			}
			else if (navigationResult == NavigationResult.Backward)
			{
				_previousHandler?.Invoke(input, EventArgs.Empty);
			}
			else if (navigationResult == NavigationResult.Forward)
			{
				_nextHandler?.Invoke(input, EventArgs.Empty);
			}
		}

		public virtual void SetNavigationVisibility(Input input)
		{
			if (_navigationStack != null)
			{
				if (_showNavigationButtons == ShowNavigationOptions.WhenComplete)
				{
					_navigationStack.IsVisible = input.Complete && _inputGroup.Inputs.Where(x => x.Required).All(x => x.Complete);
				}
				else if (_showNavigationButtons == ShowNavigationOptions.WhenValid)
				{
					_navigationStack.IsVisible = input.Valid && _inputGroup.Inputs.All(x => x.Valid);
				}
				else if (_showNavigationButtons == ShowNavigationOptions.WhenCorrect)
				{
					_navigationStack.IsVisible = input.Valid && _inputGroup.Inputs.All(x => x.Correct);
				}
			}
		}

		protected override bool OnBackButtonPressed()
		{
			// the only applies to phones with a hard/soft back button. iOS does not have this button. on
			// android, allow the user to cancel/pause the page with the back button.

			if (_canNavigateBackward)
			{
				Navigate(NavigationResult.Backward);
			}
			else
			{
				Interrupt(true, true);
			}

			return true;
		}

		private void HandleStaleNavigation()
		{
			SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
			{
				if (_responseTaskCompletionSource.Task.IsCompleted && ReturnPage != null)
				{
					App app = Application.Current as App;

					app.DetailPage = ReturnPage;
				}
			});
		}

		public async Task PrepareAsync()
		{
			foreach (Input displayedInput in _displayedInputs)
			{
				await displayedInput.PrepareAsync();
			}

			if (_inputGroup.Timeout != null)
			{
				_timer = new Timer(_inputGroup.Timeout.Value * 1000) { AutoReset = false };

				_timer.Elapsed += (o, e) =>
				{
					SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
					{
						if (IsLastPage || _inputGroup.ShowNavigationButtons == ShowNavigationOptions.AfterTimeout)
						{
							if (_navigationStack != null)
							{
								_navigationStack.IsVisible = true;
							}
						}
						else
						{
							_responseTaskCompletionSource.TrySetResult(NavigationResult.Timeout);
						}
					});
				};

				_timer.Start();
			}
		}

		public async Task DisposeAsync()
		{
			// the page is disappearing, so dispose of inputs
			foreach (Input displayedInput in _displayedInputs)
			{
				await displayedInput.DisposeAsync(await ResponseTask);
			}

			if (_timer != null)
			{
				_timer.Stop();
			}
		}

		public void Interrupt(bool useHandler, bool useReturnPage)
		{
			if (_responseTaskCompletionSource.Task.IsCompleted == false)
			{
				if (useReturnPage == false)
				{
					ReturnPage = null;
				}

				if (_savedState)
				{
					_responseTaskCompletionSource.TrySetResult(NavigationResult.Paused);
				}
				else if (useHandler)
				{
					_cancelHandler?.Invoke(this, EventArgs.Empty);
				}
				else
				{
					Navigate(NavigationResult.Cancel);
				}
			}
		}
	}
}
