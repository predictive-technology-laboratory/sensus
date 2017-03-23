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
using Newtonsoft.Json;
using Sensus.UI.UiProperties;

namespace Sensus.UI.Inputs
{
    public class SliderInput : Input
    {
        public const string EFFECT_RESOLUTION_EFFECT_NAME = "SliderInputEffect";
        public const string EFFECT_RESOLUTION_NAME = EFFECT_RESOLUTION_GROUP_NAME + "." + EFFECT_RESOLUTION_EFFECT_NAME;

        private string _tipText;
        private double _minimum;
        private double _maximum;
        private double _increment;
        private string _leftLabel;
        private string _rightLabel;
        private bool _displaySliderValue;
        private bool _displayMinMax;
        private Slider _slider;
        private double _incrementalValue;
        private bool _incrementalValueHasChanged;
        private Label _sliderLabel;

        [EntryStringUiProperty("Tip Text:", true, 9)]
        public string TipText
        {
            get
            {
                return _tipText;
            }
            set
            {
                _tipText = value?.Trim();
            }
        }

        [EntryDoubleUiProperty(null, true, 10)]
        public double Minimum
        {
            get
            {
                return _minimum;
            }
            set
            {
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

        [EntryStringUiProperty("Left Label:", true, 13)]
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

        [EntryStringUiProperty("Right Label:", true, 14)]
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

        [OnOffUiProperty("Display Slider Value:", true, 15)]
        public bool DisplaySliderValue
        {
            get
            {
                return _displaySliderValue;
            }
            set
            {
                _displaySliderValue = value;
            }
        }

        [OnOffUiProperty("Display Min and Max:", true, 16)]
        public bool DisplayMinMax
        {
            get
            {
                return _displayMinMax;
            }
            set
            {
                _displayMinMax = value;
            }
        }

        public override object Value
        {
            get
            {
                // the slider can be untouched but still have a value associated with it (i.e., the position of the slider). if the slider
                // is not a required input, then this value would be returned, which is not what we want since the user never interacted with the
                // input. so, additionally keep track of whether the value has actually changed, indicating that the user has touched the control.
                return _slider == null || !_incrementalValueHasChanged ? null : (object)_incrementalValue;
            }
        }

        [JsonIgnore]
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
                return "Slider";
            }
        }

        public SliderInput()
        {
            Construct(1, 10);
        }

        public SliderInput(string labelText, double minimum, double maximum)
            : base(labelText)
        {
            Construct(minimum, maximum);
        }

        public SliderInput(string name, string labelText, double minimum, double maximum)
            : base(name, labelText)
        {
            Construct(minimum, maximum);
        }

        private void Construct(double minimum, double maximum)
        {
            _tipText = "Please tap the range below to select a value";
            _minimum = minimum;
            _maximum = maximum;
            _increment = (_maximum - _minimum + 1) / 10;
            _leftLabel = _rightLabel = null;
            _displaySliderValue = true;
            _displayMinMax = true;
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                _slider = new Slider
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,

                    // need to set the min and max to extremes to allow them to be reset below to arbitrary values
                    Minimum = double.MinValue,
                    Maximum = double.MaxValue

                    // set the style ID on the view so that we can retrieve it when unit testing
#if UNIT_TESTING
                    , StyleId = Name
#endif
                };

                _incrementalValueHasChanged = false;

                _sliderLabel = CreateLabel(index);
                if (!string.IsNullOrWhiteSpace(_tipText))
                {
                    _sliderLabel.Text += " (" + _tipText + ")";
                }

                _slider.Minimum = _minimum;
                _slider.Maximum = _maximum;
                _slider.ValueChanged += (sender, e) =>
                {
                    double newIncrementalValue = GetIncrementalValue(e.NewValue);

                    if (newIncrementalValue != _incrementalValue)
                    {
                        _incrementalValue = newIncrementalValue;
                        _incrementalValueHasChanged = true;
                        _sliderLabel.Text = _displaySliderValue ? GetLabelText(index) + "  " + _incrementalValue : GetLabelText(index);
                        Complete = Value != null;
                    }
                };

                // we use the effects framework to hide the slider's initial position from the user, in order to avoid biasing the user away from or toward the initial position.
                _slider.Effects.Add(Effect.Resolve(EFFECT_RESOLUTION_NAME));

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children =
                    {
                        _sliderLabel,
                        new StackLayout
                        {
                            Orientation = StackOrientation.Horizontal,
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            Children =
                            {
                                new Label
                                {
                                    Text = _displayMinMax ? _slider.Minimum.ToString() : " ", // we used to set the label invisible, but this doesn't leave enough vertical space above/below the slider. adding a blank space does the trick.
                                    FontSize = 20,
                                    HorizontalOptions = LayoutOptions.Fill
                                },
                                _slider,
                                new Label
                                {
                                    Text = _displayMinMax ? _slider.Maximum.ToString() : " ",  // we used to set the label invisible, but this doesn't leave enough vertical space above/below the slider. adding a blank space does the trick.
                                    FontSize = 20,
                                    HorizontalOptions = LayoutOptions.Fill
                                }
                            }
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
                                    FontSize = 15,
                                    HorizontalOptions = LayoutOptions.FillAndExpand
                                },
                                new Label
                                {
                                    Text = _rightLabel,
                                    FontSize = 15,
                                    HorizontalOptions = LayoutOptions.End
                                }
                            },
                            IsVisible = (!string.IsNullOrWhiteSpace(_leftLabel) || !string.IsNullOrWhiteSpace(_rightLabel))
                        }
                    }
                });
            }
            else
            {
                if (Enabled)
                {
                    // if the view was already initialized and is enabled, just update the label since the index might have changed.
                    string tipText = _incrementalValueHasChanged ? "" : " " + _tipText;
                    _sliderLabel.Text = GetLabelText(index) + (_displaySliderValue && _incrementalValueHasChanged ? "  " + _incrementalValue.ToString() : "") + tipText;
                }
                else
                {
                    // if the view was already initialized but is not enabled and has never been interacted with, there should be no tip text since the user can't do anything with the slider.
                    if (!_incrementalValueHasChanged)
                    {
                        _slider.Value = _minimum;
                        _sliderLabel.Text = GetLabelText(index) + "  No value selected.";
                    }
                }
            }

            return base.GetView(index);
        }

        private double GetIncrementalValue(double value)
        {
            return Math.Round(value / _increment) * _increment;
        }

        public override string ToString()
        {
            return base.ToString() + " -- " + _minimum + " to " + _maximum;
        }
    }
}