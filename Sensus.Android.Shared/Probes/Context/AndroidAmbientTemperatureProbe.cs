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
using Android.Hardware;
using Sensus.Probes.Context;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Context
{
    public class AndroidAmbientTemperatureProbe : ListeningAmbientTemperatureProbe
    {
        private AndroidSensorListener _temperatureListener;

        public AndroidAmbientTemperatureProbe()
        {
            _temperatureListener = new AndroidSensorListener(SensorType.AmbientTemperature, async e =>
            {
                // looks like it's very risky to use e.Timestamp as the basis for timestamping our Datum objects. depending on the phone
                // manufacturer and android version, e.Timestamp will be set relative to different anchors. this makes it impossible to
                // compare data across sensors, phones, and android versions. using DateTimeOffset.UtcNow will cause imprecision due to
                // latencies between reading time and execution time of the following line of code; however, these will be small under
                // most conditions. one exception is when the listening probe lets the device sleep. in this case readings will be paused
                // until the cpu wakes up, at which time any cached readings will be delivered in bulk to sensus. each of these readings
                // will be timestamped with similar times by the following line of code, when in reality they originated much earlier. this
                // will only happen when all listening probes are configured to allow the device to sleep.
                await StoreDatumAsync(new AmbientTemperatureDatum(DateTimeOffset.UtcNow, e.Values[0]));
            });
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _temperatureListener.Initialize(MinDataStoreDelay);
        }

        protected override Task StartListeningAsync()
        {
            _temperatureListener.Start();
            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            _temperatureListener.Stop();
            return Task.CompletedTask;
        }
    }
}
