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

namespace Sensus.UI.Inputs
{
	public enum ButtonStates
	{
		Default,
		Correct,
		Incorrect,
		Selectable,
		Selected
	}

	public class ButtonWithValue : Button
	{
		public ButtonWithValue()
		{
			//StyleClass = new List<string> { "ButtonWithValue" };
		}

		public string Value { get; set; }

		public static readonly BindableProperty StateProperty = BindableProperty.Create(nameof(State), typeof(ButtonStates), typeof(ButtonWithValue));

		public ButtonStates State
		{
			get => (ButtonStates)GetValue(StateProperty);
			set => SetValue(StateProperty, value);
		}
	}
}
