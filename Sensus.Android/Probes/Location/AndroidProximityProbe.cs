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
using Sensus.Probes.Location;
using System;

namespace Sensus.Android.Probes.Location
{
    public class AndroidProximityProbe : ProximityProbe
    {
        private AndroidSensorListener _accelerometerListener;
        private double _maxRange;
        public AndroidProximityProbe()
        {
            var sensorManager = ((AndroidSensusServiceHelper)SensusServiceHelper.Get()).GetSensorManager();
            var proxSensor = sensorManager.GetDefaultSensor(SensorType.Proximity);
            _maxRange = proxSensor.MaximumRange;
            _accelerometerListener = new AndroidSensorListener(SensorType.Proximity, null, e =>
            {
                // should get distance value
                if (e?.Values?.Count == null || e.Values.Count == 0)
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

                //from https://developer.android.com/guide/topics/sensors/sensors_position
                //Note: Some proximity sensors return binary values that represent "near" or "far." In this case, the sensor usually reports
                //its maximum range value in the far state and a lesser value in the near state. Typically, the far value is a value > 5 cm, 
                //but this can vary from sensor to sensor. You can determine a sensor's maximum range by using the getMaximumRange() method.

                //if it is greater or equal to the max range of the sensor then
                //lets put the maximum to indicate that it is a far reading
                double distance = e.Values[0] >= _maxRange ? double.MaxValue : e.Values[0]; 

                StoreDatum(new ProximityDatum(DateTimeOffset.UtcNow, distance));
            });
        }

        protected override void Initialize()
        {
            base.Initialize();

            _accelerometerListener.Initialize(MinDataStoreDelay);
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