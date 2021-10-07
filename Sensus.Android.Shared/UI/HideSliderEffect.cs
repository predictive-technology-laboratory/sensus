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

using Android.Widget;
using Android.Graphics.Drawables;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Sensus.Android.UI;
using Sensus.UI.Inputs;
using Android.Views;

[assembly: ExportEffect(typeof(HideSliderEffect), SliderWithOptionsInput.EFFECT_RESOLUTION_EFFECT_NAME)]

namespace Sensus.Android.UI
{
	/// <summary>
	/// Hides the slider thumb button until the user taps the slider. This is important because displaying the thumb
	/// button immediately can bias the user to (1) move the button away from the initial value or (2) bias the user
	/// to select a value near the initial value (anchor-adjustment bias).
	/// </summary>
	public class HideSliderEffect : PlatformEffect
	{
		private SeekBar _nativeSeekBar;
		private Drawable _thumbDrawable;

		protected override void OnAttached()
		{
			_nativeSeekBar = Control as SeekBar;
			
			if (_thumbDrawable == null)
			{
				_thumbDrawable = _nativeSeekBar.Thumb;
			}

			_nativeSeekBar.Progress = _nativeSeekBar.Max;
			_nativeSeekBar.SetThumb(new ColorDrawable(global::Android.Graphics.Color.Transparent));
		}

		protected override void OnDetached()
		{
			_nativeSeekBar.SetThumb(_thumbDrawable);
		}
	}
}