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

namespace SensusUI
{
    public class PromptForInputsPage : ContentPage
    {
        public enum InputType
        {
            Text,
            YesNo,
            NumberSlider,
            NumberEntry,
            ItemPicker
        }

        public PromptForInputsPage(string title, List<string> labels, List<Tuple<InputType, List<object>>> inputTypesOptions, Action<List<object>> callback)
        {
            if (labels == null || inputTypesOptions == null || labels.Count != inputTypesOptions.Count)
                throw new SensusException("Invalid construction of PromptForInputsPage.");
            
            Title = title;

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            List<Func<object>> inputRetrievers = new List<Func<object>>();

            for (int i = 0; i < labels.Count; ++i)
            {
                string label = labels[i];
                Tuple<InputType, List<object>> inputTypeOptions = inputTypesOptions[i];

                View inputView = null;
                Func<object> inputRetriever = null;

                if (inputTypeOptions.Item1 == InputType.ItemPicker)
                {
                    Picker picker = new Picker
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                    };

                    foreach (string s in inputTypeOptions.Item2)
                        picker.Items.Add(s);

                    inputView = picker;
                    inputRetriever = new Func<object>(() => picker.SelectedIndex >= 0 ? picker.Items[picker.SelectedIndex] : null);
                }
                if (inputTypeOptions.Item1 == InputType.NumberEntry)
                {
                    Entry entry = new Entry
                    {
                        Keyboard = Keyboard.Numeric,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    inputView = entry;
                    inputRetriever = new Func<object>(() =>
                        {
                            int value;
                            int.TryParse(entry.Text, out value);

                            return value;
                        });
                }
                else if (inputTypeOptions.Item1 == InputType.NumberSlider)
                {
                    float[] options = inputTypeOptions.Item2.Select(o => float.Parse(o.ToString())).ToArray();

                    Slider slider = new Slider
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Minimum = options[0],
                        Maximum = options[1]
                    };

                    inputView = slider;
                    inputRetriever = new Func<object>(() => slider.Value);
                }
                else if (inputTypeOptions.Item1 == InputType.Text)
                {
                    Entry entry = new Entry
                    {
                        
                        Keyboard = Keyboard.Default,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    inputView = entry;
                    inputRetriever = new Func<object>(() => entry.Text);
                }
                else if (inputTypeOptions.Item1 == InputType.YesNo)
                {
                    Switch toggle = new Switch();

                    inputView = toggle;
                    inputRetriever = new Func<object>(() => toggle.IsToggled);
                }
                
                contentLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children =
                        {
                            new Label
                            {
                                Text = label,
                                FontSize = 20
                            },
                            inputView
                        }
                    });

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
                List<object> inputs = new List<object>();
                foreach (Func<object> inputRetriever in inputRetrievers)
                    inputs.Add(inputRetriever());

                callback(inputs);
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