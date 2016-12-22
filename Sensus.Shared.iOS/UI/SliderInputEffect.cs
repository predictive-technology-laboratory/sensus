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
    public class SliderInputEffect : PlatformEffect, IInputEffect<Slider, double>
    {
        public event Action<double> ValueChanged;

        public void SetFormsControl(Slider formsSlider)
        {
            // do nothing. we don't need to track a reference to the forms control.
        }

        protected override void OnAttached()
        {
            UISlider nativeSlider = Control as UISlider;

            // make the thumb image invisible
            UIImage defaultThumbImage = nativeSlider.ThumbImage(UIControlState.Normal);
            nativeSlider.SetThumbImage(new UIImage(), UIControlState.Normal);
            bool resetThumbImage = true;

            // listen for the user pressing on the slider. this also reports slides.
            nativeSlider.AddGestureRecognizer(new UILongPressGestureRecognizer(pressRecognizer =>
            {
                // make the thumb image visible
                if (resetThumbImage)
                {
                    nativeSlider.SetThumbImage(defaultThumbImage, UIControlState.Normal);
                    resetThumbImage = false;
                }

                // update the slider value - we subtract 25 from the width of the frame to get the right offset (fudgey)
                CGPoint pointTapped = pressRecognizer.LocationInView(pressRecognizer.View);
                float percent = (float)((pointTapped.X - nativeSlider.Frame.Location.X) / (nativeSlider.Frame.Size.Width - 25));
                float newValue = nativeSlider.MinValue + (percent * (nativeSlider.MaxValue - nativeSlider.MinValue));
                nativeSlider.SetValue(newValue, false);

                // let the observer know that the value has changed
                ValueChanged?.Invoke(nativeSlider.Value);
            })
            { MinimumPressDuration = 0 });
        }

        protected override void OnDetached()
        {
        }
    }
}