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
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using TimePicker = Xamarin.Forms.TimePicker;
using iOSPlatform = Xamarin.Forms.PlatformConfiguration.iOS;
using System;

namespace Sensus.UI.Inputs
{
	public class TimeInput : Input, IVariableDefiningInput
	{
		private ConstrainedTimePicker _timePicker;
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

		[TimeUiProperty("Minimum Time (iOS):", true, 18, false)]
		public TimeSpan? MinimumTime { get; set; }
		[TimeUiProperty("Maximum Time (iOS):", true, 19, false)]
		public TimeSpan? MaximumTime { get; set; }

		public override object Value
		{
			get
			{
				return _timePicker == null || _hasFocused == false ? null : _timePicker?.Time;
			}
		}

		[JsonIgnore]
		public override bool Enabled
		{
			get
			{
				return _timePicker.IsEnabled;
			}
			set
			{
				_timePicker.IsEnabled = value;
			}
		}

		public override string DefaultName
		{
			get
			{
				return "Picker (Time)";
			}
		}

		public TimeInput()
		{
		}

		public TimeInput(string labelText)
			: base(labelText)
		{
		}

		public TimeInput(string labelText, string name)
			: base(labelText, name)
		{
		}

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
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
					_timePicker.MinimumTime = MinimumTime.Value;
				}

				if (MaximumTime != null)
				{
					_timePicker.MaximumTime = MaximumTime.Value;
				}

				_timePicker.On<iOSPlatform>().SetUpdateMode(UpdateMode.WhenFinished);

				Color defaultTextColor = _timePicker.TextColor;
				_timePicker.TextColor = Color.Gray;
				_hasFocused = false;

				_timePicker.Focused += (o, e) =>
				{
					if (!_hasFocused)
					{
						_timePicker.TextColor = defaultTextColor;
						_hasFocused = true;
					}
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

				base.SetView(new StackLayout
				{
					Orientation = StackOrientation.Horizontal,
					HorizontalOptions = LayoutOptions.FillAndExpand,
					Children = { _label, _timePicker }
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