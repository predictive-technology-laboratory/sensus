//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using UIKit;
using CoreGraphics;

using Sensus.iOS.UI;
using Sensus.UI.Inputs;


// register the slider input effect
[assembly: ExportEffect(typeof(SliderInputEffect), SliderInput.EFFECT_RESOLUTION_EFFECT_NAME)]

namespace Sensus.iOS.UI
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
