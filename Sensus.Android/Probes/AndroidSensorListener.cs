using Android.App;
using Android.Hardware;
using SensusService.Exceptions;
using System;
using System.Collections.Generic;

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
            _sensorManager = Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _listening = false;
        }

        public bool Initialize()
        {
            IList<Sensor> sensors = _sensorManager.GetSensorList(_sensorType);
            _sensor = null;
            if (sensors.Count > 0)
                _sensor = sensors[0];

            return _sensor != null;
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