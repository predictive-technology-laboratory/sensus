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
using Sensus.Probes.Location;
using System;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Location
{
    /// <summary>
    /// Android proximity probe. Will report distance from phone to a nearby object. Readings from this sensor
    /// will wake up the phone's processor and be delivered regardless of the phone's awake/standby status.
    /// </summary>
    public class AndroidProximityProbe : ProximityProbe
    {
        private AndroidSensorListener _proximityListener;
        private double _maximumRange;

        public AndroidProximityProbe()
        {
            _proximityListener = new AndroidSensorListener(SensorType.Proximity, async e =>
            {
                // looks like it's very risky to use e.Timestamp as the basis for timestamping our Datum objects. depending on the phone
                // manufacturer and android version, e.Timestamp will be set relative to different anchors. this makes it impossible to
                // compare data across sensors, phones, and android versions. using DateTimeOffset.UtcNow will cause imprecision due to
                // latencies between reading time and execution time of the following line of code; however, these will be small under
                // most conditions. one exception is when the listening probe lets the device sleep. in this case readings will be paused
                // until the cpu wakes up, at which time any cached readings will be delivered in bulk to sensus. each of these readings
                // will be timestamped with similar times by the following line of code, when in reality they originated much earlier. this
                // will only happen when all listening probes are configured to allow the device to sleep.

                // from https://developer.android.com/guide/topics/sensors/sensors_position
                // Note: Some proximity sensors return binary values that represent "near" or "far." In this case, the sensor usually reports
                // its maximum range value in the far state and a lesser value in the near state. Typically, the far value is a value > 5 cm, 
                // but this can vary from sensor to sensor. You can determine a sensor's maximum range by using the getMaximumRange() method.

                await StoreDatumAsync(new ProximityDatum(DateTimeOffset.UtcNow, e.Values[0], _maximumRange));
            });
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // get the maximum range of the proximity sensor. must do the following within initialize rather than 
            // in the constructor, as upon JSON deserialization we will not yet have a service helper to get.
            SensorManager sensorManager = ((AndroidSensusServiceHelper)SensusServiceHelper.Get()).GetSensorManager();
            Sensor proximitySensor = sensorManager.GetDefaultSensor(SensorType.Proximity);
            _maximumRange = proximitySensor.MaximumRange;

            // initialize the listener
            _proximityListener.Initialize(MinDataStoreDelay);
        }

        protected override Task StartListeningAsync()
        {
            _proximityListener.Start();
            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            _proximityListener.Stop();
            return Task.CompletedTask;
        }
    }
}
