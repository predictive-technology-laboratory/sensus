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
using System.Linq;
using System.Threading.Tasks;
using Android.Hardware;
using Sensus;
using Sensus.Probes.Location;
using Syncfusion.SfChart.XForms;

namespace Sensus.Android.Probes.Location
{
    public class AndroidCompassProbe : CompassProbe
    {
        private AndroidSensorListener _magnetometerListener;
        private float[] _magneticFieldValues;

        private AndroidSensorListener _accelerometerListener;
        private float[] _accelerometerValues;

        public AndroidCompassProbe()
        {
            _magneticFieldValues = new float[3];
            _magnetometerListener = new AndroidSensorListener(SensorType.MagneticField, async e =>
            {
                if (e.Values != null && e.Values.Count == 3)
                {
                    await StoreHeadingAsync(magneticFieldValues: e.Values.ToArray());
                }
            });

            _accelerometerValues = new float[3];
            _accelerometerListener = new AndroidSensorListener(SensorType.Accelerometer, async e =>
            {
                if (e.Values != null && e.Values.Count == 3)
                {
                    await StoreHeadingAsync(accelerometerValues: e.Values.ToArray());
                }
            });
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _magnetometerListener.Initialize(MinDataStoreDelay);
            _accelerometerListener.Initialize(MinDataStoreDelay);
        }

        protected override Task StartListeningAsync()
        {
            _magnetometerListener.Start();
            _accelerometerListener.Start();
            return Task.CompletedTask;
        }

        private async Task StoreHeadingAsync(float[] magneticFieldValues = null, float[] accelerometerValues = null)
        {
            float[] rotationMatrix = new float[9];

            // if either the accelerometer or magnetic field values are missing, use the old ones
            if (SensorManager.GetRotationMatrix(rotationMatrix, null, accelerometerValues ?? _accelerometerValues, magneticFieldValues ?? _magneticFieldValues))
            {
                float[] azimuthPitchRoll = new float[3];

                SensorManager.GetOrientation(rotationMatrix, azimuthPitchRoll);

                double heading = azimuthPitchRoll[0] * (180 / Math.PI);  // convert heading radians to heading degrees

                if (heading < 0)
                {
                    heading = 180 + (180 - Math.Abs(heading));  // convert to [0, 360] degrees from north
                }

                // looks like it's very risky to use e.Timestamp as the basis for timestamping our Datum objects. depending on the phone
                // manufacturer and android version, e.Timestamp will be set relative to different anchors. this makes it impossible to
                // compare data across sensors, phones, and android versions. using DateTimeOffset.UtcNow will cause imprecision due to
                // latencies between reading time and execution time of the following line of code; however, these will be small under
                // most conditions. one exception is when the listening probe lets the device sleep. in this case readings will be paused
                // until the cpu wakes up, at which time any cached readings will be delivered in bulk to sensus. each of these readings
                // will be timestamped with similar times by the following line of code, when in reality they originated much earlier. this
                // will only happen when all listening probes are configured to allow the device to sleep.
                await StoreDatumAsync(new CompassDatum(DateTimeOffset.UtcNow, heading));
            }

            // update values for next call
            if (magneticFieldValues != null)
            {
                _magneticFieldValues = magneticFieldValues;
            }

            if (accelerometerValues != null)
            {
                _accelerometerValues = accelerometerValues;
            }
        }

        protected override Task StopListeningAsync()
        {
            _magnetometerListener.Stop();
            _accelerometerListener.Stop();
            return Task.CompletedTask;
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }
    }
}
