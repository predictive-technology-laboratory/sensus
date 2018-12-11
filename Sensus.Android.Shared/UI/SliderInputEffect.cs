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

using Android.Widget;
using Android.Graphics.Drawables;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Sensus.Android.UI;
using Sensus.UI.Inputs;

[assembly: ExportEffect(typeof(SliderInputEffect), SliderInput.EFFECT_RESOLUTION_EFFECT_NAME)]

namespace Sensus.Android.UI
{
    /// <summary>
    /// Hides the slider thumb button until the user taps the slider. This is important because displaying the thumb
    /// button immediately can bias the user to (1) move the button away from the initial value or (2) bias the user
    /// to select a value near the initial value (anchor-adjustment bias).
    /// </summary>
    public class SliderInputEffect : PlatformEffect
    {
        private bool _userHasChangedSliderValue;
        private Drawable _visibleThumbDrawable;

        public SliderInputEffect()
        {
            _userHasChangedSliderValue = false;
        }

        protected override void OnAttached()
        {
            SeekBar nativeSeekBar = Control as SeekBar;
            Slider formsSlider = Element as Slider;

            // make the thumb image invisible if the user hasn't previously changed the slider's value
            if (!_userHasChangedSliderValue)
            {
                _visibleThumbDrawable = nativeSeekBar.Thumb;
                nativeSeekBar.SetThumb(new ColorDrawable(global::Android.Graphics.Color.Transparent));
            }

            nativeSeekBar.ProgressChanged += (sender, e) =>
            {
                // make the thumb image visible if we haven't already
                if (!_userHasChangedSliderValue)
                {
                    nativeSeekBar.SetThumb(_visibleThumbDrawable);
                    _userHasChangedSliderValue = true;
                }

                // set the value on the forms slider control. note that SeekBars have a fixed minimum (0) and
                // a variable maximum. rescale the SeekBar's value to be in the range desired for the forms control.
                double percent = nativeSeekBar.Progress / (double)nativeSeekBar.Max;
                formsSlider.Value = formsSlider.Minimum + (percent * (formsSlider.Maximum - formsSlider.Minimum));
            };
        }

        protected override void OnDetached()
        {
        }
    }
}
