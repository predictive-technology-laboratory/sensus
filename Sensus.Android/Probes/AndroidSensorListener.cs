using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Hardware;
using Sensus.Exceptions;

namespace Sensus.Android.Probes
{
    public class AndroidSensorListener : Java.Lang.Object, ISensorEventListener
    {
        private SensorType _sensorType;
        private SensorManager _sensorManager;
        private Sensor _sensor;
        private SensorDelay _sensorDelay;
        private Action<SensorStatus> _sensorAccuracyChangedCallback;
        private Action<SensorEvent> _sensorValueChangedCallback;
        private bool _listening;

        public bool Supported
        {
            get { return _sensor != null; }
        }

        public AndroidSensorListener(SensorType sensorType, SensorDelay sensorDelay, Action<SensorStatus> sensorAccuracyChangedCallback, Action<SensorEvent> sensorValueChangedCallback)
        {
            _sensorType = sensorType;
            _sensorDelay = sensorDelay;
            _sensorManager = Application.Context.GetSystemService(Context.SensorService) as SensorManager;
            IList<Sensor> orientationSensors = _sensorManager.GetSensorList(sensorType);
            if (orientationSensors.Count > 0)
                _sensor = orientationSensors[0];

            _sensorAccuracyChangedCallback = sensorAccuracyChangedCallback;
            _sensorValueChangedCallback = sensorValueChangedCallback;
            _listening = false;
        }

        public void Start()
        {
            if (!Supported)
                throw new SensusException("Android sensor " + _sensorType + " is unsupported on this device.");

            lock (this)
            {
                if (_listening)
                    return;

                _listening = true;
            }

            _sensorManager.RegisterListener(this, _sensor, _sensorDelay);
        }

        public void Stop()
        {
            if (!Supported)
                throw new SensusException("Android sensor " + _sensorType + " is unsupported on this device.");

            lock (this)
            {
                if (!_listening)
                    return;

                _listening = false;
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
            if (_sensorValueChangedCallback != null)
                _sensorValueChangedCallback(e);
        }
    }
}