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
using System.Threading.Tasks;

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
		private string _otherResponseValue;

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

		public bool AutoSizeOptionButtons { get; set; }

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

		[OnOffUiProperty("Split into Value/Text Pairs:", true, 17)]
		public bool SplitValueTextPairs { get; set; }

		[EditableListUiProperty("\"Other\" Values:", true, 17, true)]
		public List<string> OtherValues { get; set; }

		[EntryStringUiProperty("\"Other\" Response Label:", true, 17, true)]
		public string OtherResponseLabel { get; set; }

		[OnOffUiProperty("Show Long Text:", true, 4)]
		public bool ShowLongText { get; set; }

		private const int LONG_TEXT_LENGTH = 30;

		public override object Value
		{
			get
			{
				if (string.IsNullOrEmpty(_otherResponseValue) == false)
				{
					_value = new object[] { _value, _otherResponseValue };
				}

				return _value;
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
				if (_slider != null)
				{
					_slider.IsEnabled = value;
				}

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

		private async Task<bool> HandleLongText(ButtonWithValue button)
		{
			Page page = Application.Current.MainPage.Navigation?.NavigationStack?.LastOrDefault() ?? Application.Current.MainPage;

			return button.State == ButtonStates.Selected || button.Text.Length < LONG_TEXT_LENGTH || ShowLongText == false || await page.DisplayAlert("Do you want to select:", button.Text, "Yes", "No");
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

				Label optionsLabel = new Label
				{
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					FontSize = 18,
					Text = "More choices ...",
					StyleClass = new[] { "LessDimmedText" },
					TextDecorations = TextDecorations.Underline
				};

				TapGestureRecognizer gesture = new TapGestureRecognizer()
				{
					NumberOfTapsRequired = 1
				};

				gesture.Tapped += (s, e) =>
				{
					if (_slider.Effects.Contains(_effect) == false)
					{
						_slider.Effects.Add(_effect);
					}

					sliderValueLabel.IsVisible = false;

					optionsLabel.IsVisible = false;
					_grid.IsVisible = true;
				};

				optionsLabel.GestureRecognizers.Add(gesture);

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

				HashSet<string> otherValues = OtherValues?.ToHashSet() ?? new HashSet<string>();

				StackLayout otherLayout = null;
				Label otherLabel = null;
				Editor otherEditor = null;

				_slider.DragStarted += (sender, e) =>
				{
					if (_slider.Effects.Contains(_effect))
					{
						_slider.Effects.Remove(_effect);
					}
				};

				_slider.DragCompleted += (sender, e) =>
				{
					_slider.Value = _incrementalValue;
				};

				_slider.ValueChanged += (sender, e) =>
				{
					double newIncrementalValue = GetIncrementalValue(e.NewValue);

					if (newIncrementalValue != _incrementalValue)
					{
						_incrementalValue = newIncrementalValue;
						_incrementalValueHasChanged = true;

						_value = _incrementalValue;

						if (otherLayout != null)
						{
							if (otherValues.Contains(_value.ToString()))
							{
								otherLabel.Text = $"{OtherResponseLabel ?? "Other Response"}:";

								if (otherLabel.Text.EndsWith("::"))
								{
									otherLabel.Text = otherLabel.Text[0..^1];
								}

								otherLayout.IsVisible = true;

								_otherResponseValue = otherEditor.Text;
							}
							else
							{
								otherLayout.IsVisible = false;

								_otherResponseValue = null;
							}
						}

						Complete = true;

						foreach (ButtonWithValue gridButton in _grid.Buttons)
						{
							gridButton.State = ButtonStates.Selectable;
						}

						optionsLabel.IsVisible = true;
						_grid.IsVisible = false;

						sliderValueLabel.IsVisible = DisplaySliderValue;
						sliderValueLabel.Text = _incrementalValue.ToString();
					}
				};

				// we use the effects framework to hide the slider's initial position from the user, in order to avoid biasing the user away from or toward the initial position.
				_slider.Effects.Add(_effect);

				_grid = new ButtonGridView(1, async (s, e) =>
				{
					ButtonWithValue button = (ButtonWithValue)s;

					if (await HandleLongText(button))
					{
						_value = button.Value;

						Complete = true;

						foreach (ButtonWithValue gridButton in _grid.Buttons)
						{
							gridButton.State = ButtonStates.Selectable;
						}

						button.State = ButtonStates.Selected;


						if (otherLayout != null)
						{
							bool otherSelected = otherValues.Contains(button.Value);

							if (otherSelected)
							{
								_otherResponseValue = otherEditor.Text;
							}
							else
							{
								_otherResponseValue = null;
							}

							if (string.IsNullOrWhiteSpace(OtherResponseLabel) && otherLabel != null)
							{
								otherLabel.Text = button.Text + ":";

								if (otherLabel.Text.EndsWith("::"))
								{
									otherLabel.Text = otherLabel.Text[0..^1];
								}
							}

							otherLayout.IsVisible = otherSelected;
						}
					}
				})
				{
					AutoSize = AutoSizeOptionButtons,
					IsVisible = false
				};

				foreach (string buttonValue in OtherOptions)
				{
					(string text, string value, bool isOther, bool _) = ButtonValueParser.ParseButtonValue(buttonValue, SplitValueTextPairs);

					if (isOther)
					{
						otherValues.Add(value);
					}

					ButtonWithValue button = _grid.AddButton(text, value);

					button.State = ButtonStates.Selectable;
				}

				StackLayout optionsLayout = new StackLayout
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					Orientation = StackOrientation.Vertical,
					IsVisible = OtherOptions.Any(),
					Children = { optionsLabel, _grid }
				};

				_grid.Arrange();

				if (otherValues.Any())
				{
					otherEditor = new Editor
					{
						Keyboard = Keyboard.Default,
						HorizontalOptions = LayoutOptions.FillAndExpand,
						AutoSize = EditorAutoSizeOption.TextChanges
					};

					otherEditor.Unfocused += (s, e) => _otherResponseValue = otherEditor.Text;

					otherLabel = new Label { Text = $"{OtherResponseLabel ?? "Other Response"}:" };

					if (otherLabel.Text.EndsWith("::"))
					{
						otherLabel.Text = otherLabel.Text[0..^1];
					}

					otherLayout = new StackLayout
					{
						IsVisible = false,
						Children = { otherLabel, otherEditor }
					};

					optionsLayout.Children.Add(otherLayout);
				}

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
									Text = DisplayMinMax ? _slider.Maximum.ToString() : " ", // we used to set the label invisible, but this doesn't leave enough vertical space above/below the slider. adding a blank space does the trick.
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
						optionsLayout
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