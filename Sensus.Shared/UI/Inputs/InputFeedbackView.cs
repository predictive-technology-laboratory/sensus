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
using System.Timers;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class InputFeedbackView : StackLayout
	{
		private readonly double _progressIncrement;
		private Label _feedbackLabel;
		private Timer _progressTimer;
		private ProgressBar _progressBar;

		public InputFeedbackView(double progressIncrement, string correctFeedbackMessage, double correctDelay, string incorrectFeedbackMessage, double incorrectDelay)
		{
			_progressIncrement = progressIncrement;

			CorrectFeedbackMessage = correctFeedbackMessage;
			CorrectDelay = correctDelay;
			IncorrectFeedbackMessage = incorrectFeedbackMessage;
			IncorrectDelay = incorrectDelay;

			IsVisible = false;

			if (CorrectDelay > 0 || IncorrectDelay > 0)
			{
				_progressBar = new ProgressBar();

				Children.Add(_progressBar);
			}

			if (string.IsNullOrWhiteSpace(CorrectFeedbackMessage) == false || string.IsNullOrWhiteSpace(IncorrectFeedbackMessage) == false)
			{
				_feedbackLabel = new Label();

				Children.Add(_feedbackLabel);
			}
		}

		protected virtual void StartProgress(double delay)
		{
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

			if (isCorrect && CorrectDelay > 0)
			{
				StartProgress(CorrectDelay);

				isVisible = true;
			}
			else if (isCorrect == false && IncorrectDelay > 0)
			{
				StartProgress(IncorrectDelay);

				isVisible = true;
			}
			else
			{
				StartProgress(0);
			}

			if (isCorrect && (string.IsNullOrWhiteSpace(CorrectFeedbackMessage) == false))
			{
				_feedbackLabel.Text = CorrectFeedbackMessage;

				isVisible = true;
			}
			else if (isCorrect == false && string.IsNullOrWhiteSpace(IncorrectFeedbackMessage) == false)
			{
				_feedbackLabel.Text = IncorrectFeedbackMessage;

				isVisible = true;
			}
			else
			{
				_feedbackLabel.Text = null;
			}

			IsVisible = isVisible;
		}

		public virtual void Reset()
		{
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

		public string CorrectFeedbackMessage { get; }
		public double CorrectDelay { get; }
		public string IncorrectFeedbackMessage { get; }
		public double IncorrectDelay { get; }
	}
}
