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

using Sensus;
using System;
using Android.Hardware;
using System.Threading.Tasks;

namespace Sensus.Android.Probes
{
    public class AndroidSensorListener : Java.Lang.Object, ISensorEventListener
    {
        private SensorType _sensorType;
        private TimeSpan? _sensorDelay;
        private Func<SensorEvent, Task> _sensorValueChangedCallback;
        private SensorManager _sensorManager;
        private Sensor _sensor;
        private bool _listening;

        private readonly object _locker = new object();

        public AndroidSensorListener(SensorType sensorType, Func<SensorEvent, Task> sensorValueChangedCallback)
        {
            _sensorType = sensorType;
            _sensorValueChangedCallback = sensorValueChangedCallback;
            _listening = false;
        }

        public void Initialize(TimeSpan? sensorDelay)
        {
            _sensorDelay = sensorDelay;
            _sensorManager = ((AndroidSensusServiceHelper)SensusServiceHelper.Get()).GetSensorManager();
            _sensor = _sensorManager.GetDefaultSensor(_sensorType);

            if (_sensor == null)
            {
                throw new NotSupportedException("No sensors present for sensor type " + _sensorType);
            }
        }

        public void Start()
        {
            if (_sensor == null)
            {
                return;
            }

            lock (_locker)
            {
                if (_listening)
                {
                    return;
                }
                else
                {
                    _listening = true;
                }
            }

            // use the largest delay that will provide samples at the desired rate:  https://developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-monitor
            SensorDelay sensorDelay = SensorDelay.Fastest;
            if (_sensorDelay.HasValue)
            {
                long sensorDelayMicroseconds = _sensorDelay.Value.Ticks / 10;
                if (sensorDelayMicroseconds >= 200000)
                {
                    sensorDelay = SensorDelay.Normal;
                }
                else if (sensorDelayMicroseconds >= 60000)
                {
                    sensorDelay = SensorDelay.Ui;
                }
                else if (sensorDelayMicroseconds >= 20000)
                {
                    sensorDelay = SensorDelay.Game;
                }
            }

            _sensorManager.RegisterListener(this, _sensor, sensorDelay);
        }

        public void Stop()
        {
            if (_sensor == null)
            {
                return;
            }

            lock (_locker)
            {
                if (_listening)
                {
                    _listening = false;
                }
                else
                {
                    return;
                }
            }

            _sensorManager.UnregisterListener(this);
        }

        public async void OnSensorChanged(SensorEvent e)
        {
            if (e != null && e.Values != null && e.Values.Count > 0 && _sensorValueChangedCallback != null)
            {
                await _sensorValueChangedCallback.Invoke(e);
            }
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
        }
    }
}
