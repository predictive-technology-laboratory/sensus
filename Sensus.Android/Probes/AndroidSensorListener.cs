#region copyright
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
#endregion

using Android.Hardware;
using SensusService.Exceptions;
using System;

namespace Sensus.Android.Probes
{
    public class AndroidSensorListener : Java.Lang.Object, ISensorEventListener
    {
        private SensorType _sensorType;
        private SensorDelay _sensorDelay;
        private Action<SensorStatus> _sensorAccuracyChangedCallback;
        private Action<SensorEvent> _sensorValueChangedCallback;
        private SensorManager _sensorManager;
        private Sensor _sensor;
        private bool _listening;

        public AndroidSensorListener(SensorType sensorType, SensorDelay sensorDelay, Action<SensorStatus> sensorAccuracyChangedCallback, Action<SensorEvent> sensorValueChangedCallback)
        {
            _sensorType = sensorType;
            _sensorDelay = sensorDelay;
            _sensorAccuracyChangedCallback = sensorAccuracyChangedCallback;
            _sensorValueChangedCallback = sensorValueChangedCallback;
            _listening = false;
        }

        public void Initialize()
        {
            _sensorManager = (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).Service.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;

            _sensor = _sensorManager.GetDefaultSensor(_sensorType);
            if (_sensor == null)
                throw new Exception("No sensors present for sensor type " + _sensorType);
        }

        public void Start()
        {
            if (_sensor == null)
                throw new SensusException("Android sensor " + _sensorType + " is unsupported on this device.");

            lock (this)
            {
                if (_listening)
                    return;
                else
                    _listening = true;
            }

            _sensorManager.RegisterListener(this, _sensor, _sensorDelay);
        }

        public void Stop()
        {
            if (_sensor == null)
                throw new SensusException("Android sensor " + _sensorType + " is unsupported on this device.");

            lock (this)
            {
                if (_listening)
                    _listening = false;
                else
                    return;
            }

            _sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            if (_sensorAccuracyChangedCallback != null)
                _sensorAccuracyChangedCallback(accuracy);
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (_sensorValueChangedCallback != null && e != null && e.Values != null && e.Values.Count > 0)
                _sensorValueChangedCallback(e);
        }
    }
}