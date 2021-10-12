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
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Sensus.iOS.UI;
using Sensus.UI.Inputs;


// register the slider input effect
[assembly: ExportEffect(typeof(HideSliderEffect), SliderWithOptionsInput.EFFECT_RESOLUTION_EFFECT_NAME)]

namespace Sensus.iOS.UI
{
	public class HideSliderEffect : PlatformEffect
	{
		private UIImage _visibleThumbImage;
		private UISlider _nativeSlider;

		protected override void OnAttached()
		{
			_nativeSlider = Control as UISlider;

			if (_visibleThumbImage == null)
			{
				_visibleThumbImage = _nativeSlider.ThumbImage(UIControlState.Normal);
			}

			/*_nativeSlider.SetValue(_nativeSlider.MaxValue, false);
			_nativeSlider.SetThumbImage(new UIImage(), UIControlState.Normal);*/

			_nativeSlider.AddGestureRecognizer(new UILongPressGestureRecognizer(pressRecognizer =>
			{
				_nativeSlider.SetThumbImage(_visibleThumbImage, UIControlState.Normal);
			})
			{ MinimumPressDuration = 0 });
		}

		protected override void OnDetached()
		{
			//_nativeSlider.SetThumbImage(_visibleThumbImage, UIControlState.Normal);
		}
	}
}