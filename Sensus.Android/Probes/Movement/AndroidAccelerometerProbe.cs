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
using SensusService.Probes.Movement;
using System;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidAccelerometerProbe : AccelerometerProbe
    {
        private AndroidSensorListener _accelerometerListener;
        private float[] _gravity;

        public AndroidAccelerometerProbe()
        {
            _gravity = new float[3];

            _accelerometerListener = new AndroidSensorListener(SensorType.Accelerometer, SensorDelay.Normal, null, e =>
                {
                    if (e.Values.Count != 3)
                        return;

                    // http://developer.android.com/guide/topics/sensors/sensors_motion.html#sensors-motion-accel

                    float alpha = 0.8f;

                    _gravity[0] = alpha * _gravity[0] + (1 - alpha) * e.Values[0];
                    _gravity[1] = alpha * _gravity[1] + (1 - alpha) * e.Values[1];
                    _gravity[2] = alpha * _gravity[2] + (1 - alpha) * e.Values[2];

                    float xAccel = e.Values[0] - _gravity[0];
                    float yAccel = e.Values[1] - _gravity[1];
                    float zAccel = e.Values[2] - _gravity[2];

                    StoreDatum(new AccelerometerDatum(this, DateTimeOffset.UtcNow, xAccel, yAccel, zAccel));
                });
        }

        protected override void Initialize()
        {
            base.Initialize();

            _accelerometerListener.Initialize();
        }

        protected override void StartListening()
        {
            _accelerometerListener.Start();
        }

        protected override void StopListening()
        {
            _accelerometerListener.Stop();
        }
    }
}
