﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using System.Threading;
using System.Threading.Tasks;
using Sensus.Context;
using Xamarin.Forms;
using System.Linq;

namespace Sensus.UI
{
    public class ProgressPage : ContentPage
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ProgressBar _progressBar;
        private Label _progressBarLabel;
        private bool _displayed;

        public ProgressPage(string description, CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;

            _progressBar = new ProgressBar
            {
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            _progressBarLabel = new Label
            {
                FontSize = 20,
                HorizontalOptions = LayoutOptions.CenterAndExpand
            };

            Button cancelButton = new Button
            {
                Text = "Cancel",
                FontSize = 25,
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
                        Text = description,
                        FontSize = 25,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        VerticalOptions = LayoutOptions.CenterAndExpand
                    },
                    _progressBar,
                    _progressBarLabel,
                    cancelButton
                }
            };
        }

        public async Task DisplayAsync(INavigation navigation)
        {
            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                await navigation.PushModalAsync(this);
                _displayed = true;
                await SetProgressAsync(0, null);
            });
        }

        public double GetProgress()
        {
            return SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                return _progressBar.Progress;
            });
        }

        public async Task SetProgressAsync(double progress, string currentTask)
        {
            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                _progressBarLabel.Text = "Progress:  " + Math.Round(progress * 100) + "%" + (string.IsNullOrWhiteSpace(currentTask) ? "" : " (" + currentTask + ")");
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

        public async Task CloseAsync()
        {
            if (_displayed)
            {
                await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    if (Navigation.ModalStack.LastOrDefault() == this)
                    {
                        await Navigation.PopModalAsync();
                    }

                    _displayed = false;
                });
            }
        }
    }
}