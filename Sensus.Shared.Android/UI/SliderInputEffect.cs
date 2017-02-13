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
using Android.Widget;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using Android.Graphics.Drawables;
using Sensus.Shared.Android.UI;
using Sensus.UI.Inputs;

[assembly: ExportEffect(typeof(SliderInputEffect), SliderInput.EFFECT_RESOLUTION_EFFECT_NAME)]

namespace Sensus.Shared.Android.UI
{
    /// <summary>
    /// Hides the slider thumb button until the user taps the slider. This is important because displaying the thumb
    /// button immediately can bias the user to (1) move the button away from the initial value or (2) bias the user
    /// to select a value near the initial value (anchor-adjustment bias).
    /// </summary>
    public class SliderInputEffect : PlatformEffect, IInputEffect<Slider, double>
    {
        public event Action<double> ValueChanged;

        private Slider _formsSlider;
        private bool _userHasChangedSliderValue;
        private Drawable _visibleThumbDrawable;

        public SliderInputEffect()
        {
            _userHasChangedSliderValue = false;
        }

        public void SetFormsControl(Slider formsSlider)
        {
            _formsSlider = formsSlider;
        }

        protected override void OnAttached()
        {
            SeekBar nativeSeekBar = Control as SeekBar;

            // make the thumb image invisible if the user hasn't changed the slider's value
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

                // let the observer know that the value has changed. note that SeekBars have a fixed minimum (0) and
                // a variable maximum. rescale the SeekBar's value to be in the range desired for the forms control.
                double percent = nativeSeekBar.Progress / (double)nativeSeekBar.Max;
                _formsSlider.Value = _formsSlider.Minimum + (percent * (_formsSlider.Maximum - _formsSlider.Minimum));
                ValueChanged?.Invoke(_formsSlider.Value);
            };
        }

        protected override void OnDetached()
        {
        }
    }
}