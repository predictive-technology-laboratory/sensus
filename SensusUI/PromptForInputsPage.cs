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

namespace SensusUI
{
    public class PromptForInputsPage : ContentPage
    {
        public PromptForInputsPage(string title, List<Input> inputs, Action<List<object>> callback)
        {
            Title = title;

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            List<Func<object>> inputRetrievers = new List<Func<object>>();

            foreach (Input input in inputs)
            {

                Func<object> inputRetriever = null;

                if (input is ItemPickerInput)
                {
                    Picker picker = input.View as Picker;
                    inputRetriever = new Func<object>(() => picker.SelectedIndex >= 0 ? picker.Items[picker.SelectedIndex] : null);
                }
                else if (input is NumberEntryInput)
                {                    
                    Entry entry = input.View as Entry;
                    inputRetriever = new Func<object>(() =>
                        {
                            int value;
                            int.TryParse(entry.Text, out value);
                            return value;
                        });
                }
                else if (input is NumberSliderInput)
                {
                    Slider slider = input.View as Slider;
                    inputRetriever = new Func<object>(() => slider.Value);
                }
                else if (input is TextInput)
                {
                    Entry entry = input.View as Entry;
                    inputRetriever = new Func<object>(() => entry.Text);
                }
                else if (input is YesNoInput)
                {
                    Switch toggle = input.View as Switch;
                    inputRetriever = new Func<object>(() => toggle.IsToggled);
                }
                else if (input is LabelOnlyInput)
                {
                    // there is no view for label-only inputs -- just display the label
                }
                else
                    throw new SensusException("Unrecognized input type:  " + input.GetType().FullName);

                StackLayout inputStack = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                };

                inputStack.Children.Add(new Label
                    {
                        Text = input.Label,
                        FontSize = 20
                    });

                if (input.View != null)
                    inputStack.Children.Add(input.View);

                contentLayout.Children.Add(inputStack);

                inputRetrievers.Add(inputRetriever);
            }

            Button cancelButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = "Cancel"
            };

            cancelButton.Clicked += async (o, e) =>
            {
                await Navigation.PopAsync();
            };

            Button okButton = new Button
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20,
                Text = "OK"
            };

            okButton.Clicked += async (o, e) =>
            {
                await Navigation.PopAsync();
                List<object> inputValues = new List<object>();
                foreach (Func<object> inputRetriever in inputRetrievers)
                    inputValues.Add(inputRetriever());

                callback(inputValues);
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