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

using Android.Hardware;
using Sensus.Exceptions;
using Sensus.Probes.Movement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidGyroscopeProbe : GyroscopeProbe
    {
        private AndroidSensorListener _gyroscopeListener;

        public AndroidGyroscopeProbe()
        {
            _gyroscopeListener = new AndroidSensorListener(SensorType.Gyroscope, async e =>
            {
                if (e.Values.Count >= 3)
                {
                    await StoreDatumAsync(new GyroscopeDatum(DateTimeOffset.UtcNow, e.Values[0], e.Values[1], e.Values[2]));
                }
            });
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _gyroscopeListener.Initialize(MinDataStoreDelay);
        }

        protected override Task StartListeningAsync()
        {
            _gyroscopeListener.Start();

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            _gyroscopeListener.Stop();

            return Task.CompletedTask;
        }
    }
}
