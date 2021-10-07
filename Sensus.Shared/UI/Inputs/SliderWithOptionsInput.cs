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
using System.Collections.Generic;
using System.Linq;

namespace Sensus.UI.Inputs
{
	public class SliderWithOptionsInput : Input, IVariableDefiningInput
	{
		public const string EFFECT_RESOLUTION_EFFECT_NAME = "HideSliderEffect";
		public const string EFFECT_RESOLUTION_NAME = EFFECT_RESOLUTION_GROUP_NAME + "." + EFFECT_RESOLUTION_EFFECT_NAME;

		private readonly Effect _effect = Effect.Resolve(EFFECT_RESOLUTION_NAME);

		private string _tipText;
		private Slider _slider;
		private double _incrementalValue;
		private bool _incrementalValueHasChanged;
		private Label _sliderLabel;
		private ButtonGridView _grid;
		private string _definedVariable;
		private object _value;

		/// <summary>
		/// A short tip that explains how to pick an item from the dialog window.
		/// </summary>
		/// <value>The tip text.</value>
		[EntryStringUiProperty("Tip Text:", true, 9, false)]
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

		/// <summary>
		/// Minimum value available on the slider.
		/// </summary>
		/// <value>The minimum.</value>
		[EntryDoubleUiProperty(null, true, 10, true)]
		public double Minimum { get; set; }

		/// <summary>
		/// Maximum value available on the slider.
		/// </summary>
		/// <value>The maximum.</value>
		[EntryDoubleUiProperty(null, true, 11, true)]
		public double Maximum { get; set; }

		/// <summary>
		/// How much the slider's value should change between points.
		/// </summary>
		/// <value>The increment.</value>
		[EntryDoubleUiProperty(null, true, 12, true)]
		public double Increment { get; set; }

		/// <summary>
		/// Label to display at the left end of the slider range.
		/// </summary>
		/// <value>The left label.</value>
		[EntryStringUiProperty("Left Label:", true, 13, false)]
		public string LeftLabel { get; set; }

		/// <summary>
		/// Label to display at the right end of the slider range.
		/// </summary>
		/// <value>The left label.</value>
		[EntryStringUiProperty("Right Label:", true, 14, false)]
		public string RightLabel { get; set; }

		/// <summary>
		/// Whether or not the slider's current value should be displayed.
		/// </summary>
		/// <value><c>true</c> to display slider value; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Display Slider Value:", true, 15)]
		public bool DisplaySliderValue { get; set; }

		/// <summary>
		/// Whether or not to display the minimum and maximum values of the slider.
		/// </summary>
		/// <value><c>true</c> to display the minimum and maximum; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Display Min and Max:", true, 16)]
		public bool DisplayMinMax { get; set; }

		[EditableListUiProperty("Other Options:", true, 17, true)]
		public List<string> OtherOptions { get; set; }

		/// <summary>
		/// The name of the variable in <see cref="Protocol.VariableValueUiProperty"/> that this input should
		/// define the value for. For example, if you wanted this input to supply the value for a variable
		/// named `study-name`, then set this field to `study-name` and the user's selection will be used as
		/// the value for this variable. 
		/// </summary>
		/// <value>The defined variable.</value>
		[EntryStringUiProperty("Define Variable:", true, 2, false)]
		public string DefinedVariable
		{
			get
			{
				return _definedVariable;
			}
			set
			{
				_definedVariable = value?.Trim();
			}
		}

		public override object Value
		{
			get
			{
				// the slider can be untouched but still have a value associated with it (i.e., the position of the slider). if the slider
				// is not a required input, then this value would be returned, which is not what we want since the user never interacted with the
				// input. so, additionally keep track of whether the value has actually changed, indicating that the user has touched the control.
				return _value; // _slider == null || !_incrementalValueHasChanged ? null : (object)_incrementalValue;
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

				if (_grid != null)
				{
					_grid.IsEnabled = value;
				}
			}
		}

		public override string DefaultName
		{
			get
			{
				return "Slider (with options)";
			}
		}

		public SliderWithOptionsInput()
		{
			Construct(1, 10);
		}

		public SliderWithOptionsInput(string labelText, double minimum, double maximum)
			: base(labelText)
		{
			Construct(minimum, maximum);
		}

		public SliderWithOptionsInput(string labelText, string name, double minimum, double maximum)
			: base(labelText, name)
		{
			Construct(minimum, maximum);
		}

		private void Construct(double minimum, double maximum)
		{
			_tipText = "Please tap the range below to select a value";
			Minimum = minimum;
			Maximum = maximum;
			Increment = (Maximum - Minimum + 1) / 10;
			LeftLabel = null;
			RightLabel = null;
			DisplaySliderValue = true;
			DisplayMinMax = true;
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

					// set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
                    , StyleId = Name
#endif
				};

				Label sliderValueLabel = new Label()
				{
					FontSize = 20,
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					HorizontalTextAlignment = TextAlignment.Center,
					IsVisible = false
				};

				_incrementalValue = double.NaN;  // need this to ensure that the initial value selected by the user registers as a change.
				_incrementalValueHasChanged = false;

				_sliderLabel = CreateLabel(index);
				if (!string.IsNullOrWhiteSpace(_tipText))
				{
					_sliderLabel.Text += " (" + _tipText + ")";
				}

				_slider.Minimum = Minimum;
				_slider.Maximum = Maximum;

				// set the initial value to the minimum so that the slider bar progress fill does not reflect an 
				// initial value of zero. the thumb has been hidden, but the fill will be rendered according to this
				// value. this is needed with a slider range that has a negative minimum.
				_slider.Value = Minimum;

				_slider.ValueChanged += (sender, e) =>
				{
					double newIncrementalValue = GetIncrementalValue(e.NewValue);

					if (newIncrementalValue != _incrementalValue)
					{
						_incrementalValue = newIncrementalValue;
						_incrementalValueHasChanged = true;

						_value = _incrementalValue;
						_slider.Value = _incrementalValue;

						_slider.Effects.Remove(_effect);

						foreach (ButtonWithValue gridButton in _grid.Buttons)
						{
							gridButton.Style = null;
						}

						sliderValueLabel.IsVisible = DisplaySliderValue;
						sliderValueLabel.Text = _incrementalValue.ToString();

						Complete = true;
					}
				};

				// we use the effects framework to hide the slider's initial position from the user, in order to avoid biasing the user away from or toward the initial position.
				_slider.Effects.Add(_effect);

				_grid = new ButtonGridView(1, (s, e) =>
				{
					ButtonWithValue button = (ButtonWithValue)s;

					_value = button.Value;

					if (_slider.Effects.Contains(_effect) == false)
					{
						_slider.Effects.Add(_effect);
					}

					foreach (ButtonWithValue gridButton in _grid.Buttons)
					{
						gridButton.Style = null;
					}

					button.Style = (Style)Application.Current.Resources["SelectedButton"];

					sliderValueLabel.IsVisible = false;

					Complete = true;
				});

				foreach (string buttonValue in OtherOptions)
				{
					ButtonWithValue button = _grid.AddButton(buttonValue, buttonValue);

					button.StyleClass = new List<string>();
				}

				_grid.IsVisible = OtherOptions.Any();

				_grid.Arrange();

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
									Text = DisplayMinMax ? _slider.Minimum.ToString() : " ", // we used to set the label invisible, but this doesn't leave enough vertical space above/below the slider. adding a blank space does the trick.
									FontSize = 20,
									HorizontalOptions = LayoutOptions.Fill
								},
								_slider,
								new Label
								{
									Text = DisplayMinMax ? _slider.Maximum.ToString() : " ",  // we used to set the label invisible, but this doesn't leave enough vertical space above/below the slider. adding a blank space does the trick.
									FontSize = 20,
									HorizontalOptions = LayoutOptions.Fill
								}
							}
						},
						sliderValueLabel,
						new StackLayout
						{
							Orientation = StackOrientation.Horizontal,
							HorizontalOptions = LayoutOptions.FillAndExpand,
							Children =
							{
								new Label
								{
									Text = LeftLabel,
									FontSize = 15,
									HorizontalOptions = LayoutOptions.FillAndExpand
								},
								new Label
								{
									Text = RightLabel,
									FontSize = 15,
									HorizontalOptions = LayoutOptions.End
								}
							},
							IsVisible = !string.IsNullOrWhiteSpace(LeftLabel) || !string.IsNullOrWhiteSpace(RightLabel)
						},
						_grid
					}
				});
			}
			else
			{
				if (Enabled)
				{
					// if the view was already initialized and is enabled, just update the label since the index might have changed.
					string tipText = _incrementalValueHasChanged ? "" : " " + _tipText;
					_sliderLabel.Text = GetLabelText(index) + (DisplaySliderValue && _incrementalValueHasChanged ? "  " + _incrementalValue.ToString() : "") + tipText;
				}
				else
				{
					// if the view was already initialized but is not enabled and has never been interacted with, there should be no tip text since the user can't do anything with the slider.
					if (!_incrementalValueHasChanged)
					{
						_slider.Value = Minimum;
						_sliderLabel.Text = GetLabelText(index) + "  No value selected.";
					}
				}
			}

			return base.GetView(index);
		}

		private double GetIncrementalValue(double value)
		{
			return Math.Round(value / Increment) * Increment;
		}

		public override string ToString()
		{
			return base.ToString() + " -- " + Minimum + " to " + Maximum;
		}
	}
}