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
using SensusService.Probes.Location;
using System;

namespace Sensus.Android.Probes.Location
{
    public class AndroidCompassProbe : CompassProbe
    {
        private AndroidSensorListener _magnetometerListener;
        private float[] _magneticFieldValues;

        private AndroidSensorListener _accelerometerListener;
        private float[] _accelerometerValues;

        private float[] _rMatrix;
        private float[] _iMatrix;
        private float[] _azimuthPitchRoll;

        private readonly object _locker = new object();

        public AndroidCompassProbe()
        {
            _rMatrix = new float[9];
            _iMatrix = new float[9];
            _azimuthPitchRoll = new float[3];

            _magneticFieldValues = new float[3];
            _magnetometerListener = new AndroidSensorListener(SensorType.MagneticField, SensorDelay.Normal, null, e =>
                {
                    if (e.Values != null && e.Values.Count == 3)
                        lock (_locker)
                        {
                            for (int i = 0; i < 3; i++)
                                _magneticFieldValues[i] = e.Values[i];

                            StoreHeading();
                        }
                });

            _accelerometerValues = new float[3];
            _accelerometerListener = new AndroidSensorListener(SensorType.Accelerometer, SensorDelay.Normal, null, e =>
                {
                    if (e.Values != null && e.Values.Count == 3)
                        lock (_locker)
                        {
                            for (int i = 0; i < 3; i++)
                                _accelerometerValues[i] = e.Values[i];

                            StoreHeading();
                        }
                });
        }

        protected override void Initialize()
        {
            base.Initialize();

            _magnetometerListener.Initialize();
            _accelerometerListener.Initialize();
        }

        protected override void StartListening()
        {
            _magnetometerListener.Start();
            _accelerometerListener.Start();
        }

        private void StoreHeading()
        {
            if (SensorManager.GetRotationMatrix(_rMatrix, _iMatrix, _accelerometerValues, _magneticFieldValues))
            {
                SensorManager.GetOrientation(_rMatrix, _azimuthPitchRoll);

                double heading = _azimuthPitchRoll[0] * (180 / Math.PI);  // convert heading radians to heading degrees
                if (heading < 0)
                    heading = 180 + (180 - Math.Abs(heading));  // convert to [0, 360] degrees from north

                StoreDatum(new CompassDatum(this, DateTimeOffset.UtcNow, heading));
            }
        }

        protected override void StopListening()
        {
            _magnetometerListener.Stop();
            _accelerometerListener.Stop();
        }
    }
}
