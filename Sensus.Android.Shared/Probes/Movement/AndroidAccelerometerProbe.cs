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

using Android.Hardware;
using Sensus.Probes.Movement;
using System;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidAccelerometerProbe : AccelerometerProbe
    {
        private AndroidSensorListener _accelerometerListener;
        private const float GRAVITY = -9.80665f;

        public AndroidAccelerometerProbe()
        {
            _accelerometerListener = new AndroidSensorListener(SensorType.Accelerometer, async e =>
            {
                // should get x, y, and z values
                if (e.Values.Count != 3)
                {
                    return;
                }

                // looks like it's very risky to use e.Timestamp as the basis for timestamping our Datum objects. depending on the phone
                // manufacturer and android version, e.Timestamp will be set relative to different anchors. this makes it impossible to
                // compare data across sensors, phones, and android versions. using DateTimeOffset.UtcNow will cause imprecision due to
                // latencies between reading time and execution time of the following line of code; however, these will be small under
                // most conditions. one exception is when the listening probe lets the device sleep. in this case readings will be paused
                // until the cpu wakes up, at which time any cached readings will be delivered in bulk to sensus. each of these readings
                // will be timestamped with similar times by the following line of code, when in reality they originated much earlier. this
                // will only happen when all listening probes are configured to allow the device to sleep.
                await StoreDatumAsync(new AccelerometerDatum(DateTimeOffset.UtcNow, e.Values[0] / GRAVITY, e.Values[1] / GRAVITY, e.Values[2] / GRAVITY));
            });
        }

        protected override async Task ProtectedInitializeAsync()
        {
            await base.ProtectedInitializeAsync();

            _accelerometerListener.Initialize(MinDataStoreDelay);
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _accelerometerListener.Start();
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();

            _accelerometerListener.Stop();
        }
    }
}