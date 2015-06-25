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

namespace SensusUI.Inputs
{
    public class NumberSliderInput : Input
    {
        public NumberSliderInput(string label, double minimum, double maximum)
            : base(label)
        {
            Slider slider = new Slider
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Minimum = minimum,
                Maximum = maximum
            };

            Label sliderValueLabel = new Label
            {
                Text = slider.Value.ToString(),
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                FontSize = 20
            };

            slider.ValueChanged += (o, e) =>
            {
                sliderValueLabel.Text = e.NewValue.ToString();
            };

            View = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children =
                { 
                    Label,
                    new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { slider, sliderValueLabel }
                    }
                }
            };
            
            ValueRetriever = new Func<object>(() => slider.Value);
        }
    }
}