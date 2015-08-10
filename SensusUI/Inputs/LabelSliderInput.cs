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
    public class LabelSliderInput : Input
    {
        private double _minimum;
        private double _maximum;
        private string _leftLabel;
        private string _rightLabel;
        private double _increment;
        private Slider _slider;

        [EntryStringUiProperty("Left label:", true, 13)]
        public string LeftLabel
        {
            get
            {
                return _leftLabel;
            }
            set
            {
                _leftLabel = value;
            }
        }

        [EntryStringUiProperty("Right label:", true, 14)]
        public string RightLabel
        {
            get
            {
                return _rightLabel;
            }
            set
            {
                _rightLabel = value;
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
                            Maximum = double.MaxValue,
                        };

                    _slider.Minimum = _minimum;
                    _slider.Maximum = _maximum;

                    _slider.Value = (_maximum - _minimum) / 2d;

                    _slider.ValueChanged += (o, e) =>
                        {
                            _slider.Value = Math.Round(_slider.Value / _increment) * _increment;
                            Complete = Value != null;
                        };

                    base.View = new StackLayout
                        {
                            Orientation = StackOrientation.Vertical,
                            VerticalOptions = LayoutOptions.Start,
                            Padding = new Thickness (10, 10, 10, 10),
                            Children =
                                {
                                    Label,
                                    new StackLayout
                                    {
                                        Orientation = StackOrientation.Vertical,
                                        VerticalOptions = LayoutOptions.FillAndExpand,
                                        Children = { _slider }
                                    },
                                    new StackLayout
                                    {
                                        Orientation = StackOrientation.Horizontal,
                                        HorizontalOptions = LayoutOptions.FillAndExpand,
                                        Children = 
                                            {
                                                new Label
                                                {
                                                    Text = _leftLabel,
                                                    HorizontalOptions = LayoutOptions.FillAndExpand,
                                                    FontSize = 15
                                                },

                                                new Label
                                                {
                                                    Text = _rightLabel,
                                                    HorizontalOptions = LayoutOptions.End,
                                                    FontSize = 15
                                                }
                                            }

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
                return "Label Slider";
            }
        }

        public LabelSliderInput()
        {
            Construct(1, 100);
        }

        public LabelSliderInput(string labelText, double minimum, double maximum)
            : base(labelText)
        {
            Construct(minimum, maximum);
        }

        public LabelSliderInput(string name, string labelText, double minimum, double maximum)
            : base(name, labelText)
        {
            Construct(minimum, maximum);
        }

        private void Construct(double minimum, double maximum)
        {
            _minimum = minimum;
            _maximum = maximum;
            _increment = 1;
        }

        public override string ToString()
        {
            return base.ToString() + " -- " + _minimum + " to " + _maximum;
        }
    }
}