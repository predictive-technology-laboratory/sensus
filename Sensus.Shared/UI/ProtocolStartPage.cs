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
using System.Threading;
using System.Threading.Tasks;
using Sensus.Context;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class ProtocolStartPage : ContentPage
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ProgressBar _progressBar;
        private Label _progressBarLabel;

        public ProtocolStartPage(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;

            _progressBar = new ProgressBar()
            {
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            _progressBarLabel = new Label()
            {
                FontSize = 20,
                HorizontalOptions = LayoutOptions.CenterAndExpand
            };

            Button cancelButton = new Button()
            {
                Text = "Cancel",
                FontSize = 30,
                HorizontalOptions = LayoutOptions.CenterAndExpand
            };

            cancelButton.Clicked += (o, e) =>
            {
                cancelButton.Text = "Cancelling...";
                _cancellationTokenSource.Cancel();
            };

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                Children =
                {
                    new Label
                    {
                        Text = "Starting study. Please wait...",
                        FontSize = 30,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        VerticalOptions = LayoutOptions.CenterAndExpand
                    },
                    _progressBar,
                    _progressBarLabel,
                    cancelButton
                }
            };
        }

        public double GetProgress()
        {
            return SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                return _progressBar.Progress;
            });
        }

        public async Task SetProgressAsync(double progress)
        {
            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                _progressBarLabel.Text = $"Progress:  {Math.Round(progress * 100)}%";
                await _progressBar.ProgressTo(progress, 100, Easing.Linear);
            });
        }

        protected override bool OnBackButtonPressed()
        {
            // the only applies to phones with a hard/soft back button. iOS does not have this button. on 
            // android, allow the user to cancel the protocol start with the back button.
            _cancellationTokenSource.Cancel();

            return true;
        }
    }
}
