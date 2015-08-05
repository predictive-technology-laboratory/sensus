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
        public PromptForInputsPage(string title, double progress, InputGroup inputGroup, Action<List<object>> callback)
        {
            Title = title;

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            contentLayout.Children.Add(new ProgressBar
                {
                    Progress = progress,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                });

            List<Func<object>> inputRetrievers = new List<Func<object>>();

            foreach (Input input in inputGroup.Inputs)
            {
                Func<object> valueRetriever;
                View view = input.CreateView(out valueRetriever);

                if (view != null)
                    contentLayout.Children.Add(view);

                if (valueRetriever != null)
                    inputRetrievers.Add(valueRetriever);
            }

            bool canceled = false;

            Thread returnThread = new Thread(() =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                        {
                            await Navigation.PopAsync();
                        });

                    List<object> inputValues = null;

                    if (!canceled)
                    {
                        inputValues = new List<object>();
                        foreach (Func<object> inputRetriever in inputRetrievers)
                            inputValues.Add(inputRetriever());
                    }

                    callback(inputValues);
                });

            Button cancelButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = "Cancel"
            };

            cancelButton.Clicked += (o, e) =>
            {
                canceled = true;
                returnThread.Start();
            };

            Button okButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = "OK"
            };

            okButton.Clicked += (o, e) =>
            {
                returnThread.Start();
            };
                
            contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { cancelButton, okButton }
                });

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}