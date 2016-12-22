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
    public class SliderInputEffect : PlatformEffect, IInputEffect<float>
    {
        public event Action<float> ValueChanged;

        protected override void OnAttached()
        {
            UISlider slider = Control as UISlider;

            // make the thumb image invisible
            UIImage defaultThumbImage = slider.ThumbImage(UIControlState.Normal);
            slider.SetThumbImage(new UIImage(), UIControlState.Normal);
            bool resetThumbImage = true;

            // listen for the user pressing on the slider. this also reports slides.
            slider.AddGestureRecognizer(new UILongPressGestureRecognizer(pressRecognizer =>
            {
                // make the thumb image visible
                if (resetThumbImage)
                {
                    slider.SetThumbImage(defaultThumbImage, UIControlState.Normal);
                    resetThumbImage = false;
                }

                // update the slider value - we subtract 25 from the width of the frame to get the right offset
                CGPoint pointTapped = pressRecognizer.LocationInView(pressRecognizer.View);
                nfloat newValue = ((pointTapped.X - slider.Frame.Location.X) / (slider.Frame.Size.Width - 25)) * (slider.MaxValue - slider.MinValue);
                slider.SetValue((float)newValue, false);

                // let the observer know that the value has changed
                ValueChanged?.Invoke(slider.Value);
            })
            { MinimumPressDuration = 0 });
        }

        protected override void OnDetached()
        {
        }
    }
}