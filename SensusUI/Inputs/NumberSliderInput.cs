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
using SensusUI.UiProperties;

namespace SensusUI.Inputs
{
    public class NumberSliderInput : Input
    {
        private double _minimum;
        private double _maximum;
        private double _increment;
        private Slider _slider;

        [EntryDoubleUiProperty(null, true, 10)]
        public double Minimum
        {
            get
            {
                return _minimum;
            }
            set
            {
                if (value >= _maximum)
                {
                    UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Number slider input minimum must be less than maximum.");
                    value = _maximum - 1;
                }
                
                _minimum = value;
            }
        }

        [EntryDoubleUiProperty(null, true, 11)]
        public double Maximum
        {
            get
            {
                return _maximum;
            }
            set
            {
                if (value <= _minimum)
                {
                    UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Number slider input maximum must be greater than minimum.");
                    value = _minimum + 1;
                }
                
                _maximum = value;
            }
        }

        [EntryDoubleUiProperty(null, true, 12)]
        public double Increment
        {
            get
            {
                return _increment;
            }
            set
            {
                _increment = value;
            }
        }

        public override View View
        {
            get
            {
                if (base.View == null && _maximum > _minimum)
                {
                    _slider = new Slider
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Minimum = double.MinValue,
                        Maximum = double.MaxValue
                    };

                    _slider.Minimum = _minimum;
                    _slider.Maximum = _maximum;

                    Label sliderValueLabel = new Label
                    {
                        Text = _slider.Value.ToString(),
                        HorizontalOptions = LayoutOptions.End,
                        FontSize = 20
                    };                                

                    _slider.Value = (_maximum - _minimum) / 2d;

                    _slider.ValueChanged += (o, e) =>
                    {
                        _slider.Value = Math.Round(_slider.Value / _increment) * _increment;
                        sliderValueLabel.Text = e.NewValue.ToString();
                        Complete = Value != null;
                    };

                    base.View = new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        VerticalOptions = LayoutOptions.Start,
                        Children =
                        { 
                            Label,
                            new StackLayout
                            {
                                Orientation = StackOrientation.Horizontal,
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                                Children = { _slider, sliderValueLabel }
                            }
                        }
                    };
                }

                return base.View;
            }
        }

        public override object Value
        {
            get
            {
                return _slider == null ? null : (object)_slider.Value;
            }
        }

        public override bool Enabled
        {
            get
            {
                return _slider.IsEnabled;
            }
            set
            {
                _slider.IsEnabled = value;
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Number Slider";
            }
        }

        public NumberSliderInput()
        {
            Construct(1, 10);
        }

        public NumberSliderInput(string labelText, double minimum, double maximum)
            : base(labelText)
        {
            Construct(minimum, maximum);
        }

        public NumberSliderInput(string name, string labelText, double minimum, double maximum)
            : base(name, labelText)
        {
            Construct(minimum, maximum);
        }

        private void Construct(double minimum, double maximum)
        {
            _minimum = minimum;
            _maximum = maximum;
            _increment = (_maximum - _minimum + 1) / 10;
        }

        public override string ToString()
        {
            return base.ToString() + " -- " + _minimum + " to " + _maximum;
        }
    }
}