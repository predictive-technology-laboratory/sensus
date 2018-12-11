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

using System;
using System.Threading;
using System.Collections.Generic;
using Android.OS;
using Android.App;
using Sensus.Probes.Device;
using System.Threading.Tasks;
using System.Linq;

namespace Sensus.Android.Probes.Device
{
    public class AndroidScreenProbe : ScreenProbe
    {
        private PowerManager _powerManager;

        public AndroidScreenProbe()
        {
            _powerManager = Application.Context.GetSystemService(global::Android.Content.Context.PowerService) as PowerManager;
        }

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            bool screenOn;

            // see the Backwards Compatibility article for more information
#if __ANDROID_20__
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                screenOn = _powerManager.IsInteractive;  // API level 20
            }
            else
#endif
            {
                // ignore deprecation warning
#pragma warning disable 618
                screenOn = _powerManager.IsScreenOn;
#pragma warning restore 618
            }

            return Task.FromResult(new Datum[] { new ScreenDatum(DateTimeOffset.UtcNow, screenOn) }.ToList());
        }
    }
}
