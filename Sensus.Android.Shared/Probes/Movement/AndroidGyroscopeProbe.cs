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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidGyroscopeProbe : GyroscopeProbe
    {
        private AndroidSensorListener _gyroScopeListener;
        private static float NS2S = 1.0f / 1000000000.0f;
        private float[] deltaRotationVector = new float[4];
        private float lastCalculatedTime;

        public AndroidGyroscopeProbe()
        {
            _gyroScopeListener = new AndroidSensorListener(SensorType.Gyroscope, null, async e =>
            {

                // should get x, y, and z values
                if (e.Values.Count == 3 && lastCalculatedTime != default(float))
                {
                    var sensorEventVals = e.Values;
                    var values = CalculateGyroscope(e, sensorEventVals);

                    await StoreDatumAsync(new GyroscopeDatum(DateTimeOffset.UtcNow, values.x,values.y,values.z));

                }

                lastCalculatedTime = e.Timestamp;

            });
        }

        private (float x, float y, float z) CalculateGyroscope(SensorEvent e, IList<float> values)
        {
                    ///calculation from android documentation <see cref="https://developer.android.com/reference/android/hardware/SensorEvent#values"/>.


            float dT = (e.Timestamp - lastCalculatedTime) * NS2S;

            float x = values[0];
            float y = values[1];
            float z = values[2];

            double axisCalc = (x * x) + (y * y) + (z * z);

            float omegaMagnitude = (float)Math.Sqrt(axisCalc);

            if (omegaMagnitude > float.Epsilon)
            {
                x /= omegaMagnitude;
                y /= omegaMagnitude;
                z /= omegaMagnitude;
            }

            float thetaOverTwo = omegaMagnitude * dT / 2.0f;
            float sinThetaOverTwo = (float)Math.Sin(thetaOverTwo);
            float cosThetaOverTwo = (float)Math.Cos(thetaOverTwo);
            deltaRotationVector[0] = sinThetaOverTwo * x;
            deltaRotationVector[1] = sinThetaOverTwo * y;
            deltaRotationVector[2] = sinThetaOverTwo * z;
            deltaRotationVector[3] = cosThetaOverTwo;


            float[] deltaRotationMatrix = new float[9];

            SensorManager.GetRotationMatrixFromVector(deltaRotationMatrix, deltaRotationVector);
                        

            return (deltaRotationMatrix[0],deltaRotationMatrix[1],deltaRotationMatrix[2]);
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _gyroScopeListener.Initialize(MinDataStoreDelay);
        }

        protected override async Task StartListeningAsync()
        {
            _gyroScopeListener.Start();
        }

        protected override Task StopListeningAsync()
        {
            _gyroScopeListener.Stop();
            return Task.CompletedTask;
        }
    }
}