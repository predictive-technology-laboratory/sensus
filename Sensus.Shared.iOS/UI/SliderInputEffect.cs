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
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Xamarin.Forms;
using Sensus.Shared.iOS.UI;
using Sensus.UI.Inputs;
using CoreGraphics;

// register the slider input effect
[assembly: ExportEffect(typeof(SliderInputEffect), SliderInput.EFFECT_RESOLUTION_EFFECT_NAME)]

namespace Sensus.Shared.iOS.UI
{
    public class SliderInputEffect : PlatformEffect
    {
        private UIImage _visibleThumbImage;
        private bool _userHasChangedSliderValue;

        public SliderInputEffect()
        {
            _userHasChangedSliderValue = false;
        }

        protected override void OnAttached()
        {
            UISlider nativeSlider = Control as UISlider;
            Slider formsSlider = Element as Slider;

            // make the thumb image invisible if the user hasn't previously changed the slider's value
            if (!_userHasChangedSliderValue)
            {
                _visibleThumbImage = nativeSlider.ThumbImage(UIControlState.Normal);
                nativeSlider.SetThumbImage(new UIImage(), UIControlState.Normal);
            }

            // listen for the user pressing on the slider. this also reports slides.
            nativeSlider.AddGestureRecognizer(new UILongPressGestureRecognizer(pressRecognizer =>
            {
                // make the thumb image visible
                if (!_userHasChangedSliderValue)
                {
                    nativeSlider.SetThumbImage(_visibleThumbImage, UIControlState.Normal);
                    _userHasChangedSliderValue = true;
                }

                // update the slider value - we subtract 25 from the width of the frame to get the right offset (fudgey)
                CGPoint pointTapped = pressRecognizer.LocationInView(pressRecognizer.View);
                float percent = (float)((pointTapped.X - nativeSlider.Frame.Location.X) / (nativeSlider.Frame.Size.Width - 25));
                float newNativeValue = nativeSlider.MinValue + (percent * (nativeSlider.MaxValue - nativeSlider.MinValue));
                nativeSlider.SetValue(newNativeValue, false);
                formsSlider.Value = formsSlider.Minimum + (percent * (formsSlider.Maximum - formsSlider.Minimum));
            })
            { MinimumPressDuration = 0 });
        }

        protected override void OnDetached()
        {
        }
    }
}