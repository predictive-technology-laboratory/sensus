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

using Sensus.Context;
using System;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class FeedbackEventArgs : EventArgs
	{
		public bool IsCorrect { get; set; }
	}

	public class InputFeedbackView : StackLayout
	{
		private readonly double _progressIncrement;
		private readonly string _correctFeedbackMessage;
		private readonly double _correctDelay;
		private readonly string _incorrectFeedbackMessage;
		private readonly double _incorrectDelay;
		private readonly bool _persistAfterDelay;
		private readonly EventHandler<FeedbackEventArgs> _delayStarted;
		private readonly EventHandler<FeedbackEventArgs> _delayEnded;
		
		private bool _isCorrect;
		
		private Label _feedbackLabel;
		private Timer _progressTimer;
		private ProgressBar _progressBar;

		private void InvokeEventHandler(EventHandler<FeedbackEventArgs> handler)
		{
			if (handler != null)
			{
				foreach (EventHandler<FeedbackEventArgs> h in handler.GetInvocationList())
				{
					Task.Factory.FromAsync((a, _) => h.BeginInvoke(this, new FeedbackEventArgs { IsCorrect = _isCorrect }, a, _), h.EndInvoke, null);
				}
			}
		}

		public InputFeedbackView(double progressIncrement, string correctFeedbackMessage, double correctDelay, string incorrectFeedbackMessage, double incorrectDelay, bool persistAfterDelay, EventHandler<FeedbackEventArgs> delayStarted = null, EventHandler<FeedbackEventArgs> delayEnded = null)
		{
			_progressIncrement = progressIncrement;
			_correctFeedbackMessage = correctFeedbackMessage;
			_correctDelay = correctDelay;
			_incorrectFeedbackMessage = incorrectFeedbackMessage;
			_incorrectDelay = incorrectDelay;
			_persistAfterDelay = persistAfterDelay;
			_delayStarted = delayStarted;
			_delayEnded = delayEnded;

			IsVisible = false;

			if (_correctDelay > 0 || _incorrectDelay > 0)
			{
				_progressBar = new ProgressBar()
				{
					StyleClass = new[] { "FeedbackProgressBar" }
				};

				Children.Add(_progressBar);
			}

			if (string.IsNullOrWhiteSpace(_correctFeedbackMessage) == false || string.IsNullOrWhiteSpace(_incorrectFeedbackMessage) == false)
			{
				_feedbackLabel = new Label()
				{
					StyleClass = new[] { "FeedbackLabel" }
				};

				Children.Add(_feedbackLabel);
			}
		}

		protected virtual void StartProgress(double delay)
		{
			InvokeEventHandler(_delayStarted);

			if (_progressBar != null)
			{
				_progressBar.Progress = 0;

				if (delay > 0)
				{
					_progressBar.IsVisible = true;

					if (_progressTimer != null)
					{
						_progressTimer.Dispose();
					}

					_progressTimer = new Timer(delay * _progressIncrement);

					_progressTimer.Elapsed += (s, o) =>
					{
						SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
						{
							_progressBar.Progress += _progressIncrement;

							if (_progressBar.Progress >= 1)
							{
								try
								{
									_progressTimer.Stop();

									IsVisible = _persistAfterDelay;

									InvokeEventHandler(_delayEnded);
								}
								catch
								{

								}
							}
						});
					};
					_progressTimer.Start();
				}
				else
				{
					_progressBar.IsVisible = false;
				}
			}
		}

		public virtual void SetFeedback(bool isCorrect)
		{
			bool isVisible = false;
			string feedbackText = null;

			_isCorrect = isCorrect;

			if (isCorrect && _correctDelay > 0)
			{
				StartProgress(_correctDelay);

				isVisible = true;
			}
			else if (isCorrect == false && _incorrectDelay > 0)
			{
				StartProgress(_incorrectDelay);

				isVisible = true;
			}
			else
			{
				StartProgress(0);
			}

			if (isCorrect && (string.IsNullOrWhiteSpace(_correctFeedbackMessage) == false))
			{
				isVisible = true;
				feedbackText = _correctFeedbackMessage;
			}
			else if (isCorrect == false && string.IsNullOrWhiteSpace(_incorrectFeedbackMessage) == false)
			{
				isVisible = true;
				feedbackText = _incorrectFeedbackMessage;
			}

			IsVisible = isVisible;

			if (_feedbackLabel != null)
			{
				_feedbackLabel.Text = feedbackText;
			}
		}

		public virtual void Reset()
		{
			_isCorrect = false;

			if (_progressTimer != null)
			{
				_progressTimer.Dispose();
			}

			if (_progressBar != null)
			{
				_progressBar.Progress = 0;
			}

			if (_feedbackLabel != null)
			{
				_feedbackLabel.Text = null;
			}

			IsVisible = false;
		}
	}
}
