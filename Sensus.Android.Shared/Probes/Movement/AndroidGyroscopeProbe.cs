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
        private float timeStamp;

        public AndroidGyroscopeProbe()
        {
            _gyroScopeListener = new AndroidSensorListener(SensorType.Gyroscope, null, async e =>
            {
                // should get x, y, and z values
                if (e.Values.Count != 3 || Stabilizing || timeStamp == 0)
                {
                    return;
                }

                IList<float> sensorEventVals = e.Values;
                //method here to calculate
                float [] vals = CalculateGyroscope(e, sensorEventVals);

                await StoreDatumAsync(new GyroscopeDatum(DateTimeOffset.UtcNow, vals[0], vals[1],vals[2]));
            });
        }

        private float[] CalculateGyroscope(SensorEvent e, IList<float> values)
        {
            
            // This time step's delta rotation to be multiplied by the current rotation
            // after computing it from the gyro sample data.
            float dT = (e.Timestamp - timeStamp) * NS2S;

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
                        

            return deltaRotationMatrix;
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _gyroScopeListener.Initialize(MinDataStoreDelay);
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _gyroScopeListener.Start();
        }

        protected override Task StopListeningAsync()
        {
            _gyroScopeListener.Stop();
            return Task.CompletedTask;
        }
    }
}