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
using System.Threading;
using System.Collections.Generic;
using Android.OS;
using Android.App;
using Sensus;
using Sensus.Probes.Device;

namespace Sensus.Android.Probes.Device
{
    public class AndroidScreenProbe : ScreenProbe
    {
        private PowerManager _powerManager;

        public AndroidScreenProbe()
        {
            _powerManager = Application.Context.GetSystemService(global::Android.Content.Context.PowerService) as PowerManager;
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            bool screenOn;

            // https://github.com/predictive-technology-laboratory/sensus/wiki/Backwards-Compatibility
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

            return new Datum[] { new ScreenDatum(DateTimeOffset.UtcNow, screenOn) };
        }
    }
}
