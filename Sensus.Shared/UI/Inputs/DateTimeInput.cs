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

using Xamarin.Forms;
using Newtonsoft.Json;
using Sensus.UI.UiProperties;
using System;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using DatePicker = Xamarin.Forms.DatePicker;
using TimePicker = Xamarin.Forms.TimePicker;
using iOSPlatform = Xamarin.Forms.PlatformConfiguration.iOS;

namespace Sensus.UI.Inputs
{
	public class DateTimeInput : Input, IVariableDefiningInput
	{
		private DatePicker _datePicker;
		private TimePicker _timePicker;
		private Label _label;
		private bool _hasFocused;
		private string _definedVariable;

		/// <summary>
		/// The name of the variable in <see cref="Protocol.VariableValueUiProperty"/> that this input should
		/// define the value for. For example, if you wanted this input to supply the value for a variable
		/// named `study-name`, then set this field to `study-name` and the user's selection will be used as
		/// the value for this variable. 
		/// </summary>
		/// <value>The defined variable.</value>
		[EntryStringUiProperty("Define Variable:", true, 15, false)]
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

		[OnOffUiProperty("Require Future Date:", true, 16)]
		public virtual bool RequireFutureDate { get; set; }

		[OnOffUiProperty("Time Only:", true, 17)]
		public virtual bool TimeOnly { get; set; }

		[TimeUiProperty("Minimum Time (iOS):", true, 18, false)]
		public TimeSpan? MinimumTime { get; set; }
		[TimeUiProperty("Maximum Time (iOS):", true, 19, false)]
		public TimeSpan? MaximumTime { get; set; }

		public override object Value
		{
			get
			{
				return _datePicker == null || _timePicker == null || _hasFocused == false ? null : _datePicker?.Date.Add(_timePicker.Time);
			}
		}

		[JsonIgnore]
		public override bool Enabled
		{
			get
			{
				return _datePicker.IsEnabled && _timePicker.IsEnabled;
			}
			set
			{
				_datePicker.IsEnabled = value;
				_timePicker.IsEnabled = value;
			}
		}

		public override string DefaultName
		{
			get
			{
				return "Picker (DateTime)";
			}
		}

		public DateTimeInput()
		{
		}

		public DateTimeInput(string labelText)
			: base(labelText)
		{
		}

		public DateTimeInput(string labelText, string name)
			: base(labelText, name)
		{
		}

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				_datePicker = new DatePicker
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					
					// set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
					, StyleId = Name
#endif
				};

				_datePicker.On<iOSPlatform>().SetUpdateMode(UpdateMode.WhenFinished);

				_timePicker = new ConstrainedTimePicker
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,

					// set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
					, StyleId = Name
#endif
				};

				if (MinimumTime != null)
				{
					
				}

				_timePicker.On<iOSPlatform>().SetUpdateMode(UpdateMode.WhenFinished);

				Color defaultTextColor = _datePicker.TextColor;
				_datePicker.TextColor = Color.Gray;
				_timePicker.TextColor = Color.Gray;
				_hasFocused = false;

				_datePicker.Focused += (o, e) =>
				{
					if (_hasFocused == false)
					{
						_datePicker.TextColor = defaultTextColor;
						_timePicker.TextColor = defaultTextColor;
						_hasFocused = true;
					}

					Complete = true;
				};

				_timePicker.Focused += (o, e) =>
				{
					if (_hasFocused == false)
					{
						_datePicker.TextColor = defaultTextColor;
						_timePicker.TextColor = defaultTextColor;
						_hasFocused = true;
					}

					Complete = true;
				};

				_datePicker.DateSelected += (o, e) =>
				{
					Complete = Value != null;
				};

				_timePicker.PropertyChanged += (o, e) =>
				{
					if (e.PropertyName == nameof(TimePicker.Time))
					{
						Complete = Value != null;
					}
				};

				_label = CreateLabel(index);
				_label.VerticalTextAlignment = TextAlignment.Center;

				if (RequireFutureDate)
				{
					_datePicker.MinimumDate = DateTime.Now;
				}

				_datePicker.IsVisible = TimeOnly == false;

				base.SetView(new StackLayout
				{
					Orientation = StackOrientation.Horizontal,
					HorizontalOptions = LayoutOptions.FillAndExpand,
					Children = { _label, _datePicker, _timePicker }
				});
			}
			else
			{
				_label.Text = GetLabelText(index);  // if the view was already initialized, just update the label since the index might have changed.
			}

			return base.GetView(index);
		}
	}
}