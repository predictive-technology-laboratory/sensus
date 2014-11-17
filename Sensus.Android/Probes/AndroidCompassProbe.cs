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
using Sensus.Probes.Location;
using Android.Hardware;
using Sensus.Probes;

namespace Sensus.Android.Probes
{
    public class AndroidCompassProbe : PassiveProbe
    {
        private class CompassListener : Java.Lang.Object, ISensorEventListener
        {
            private Action<SensorEvent> _sensorChangedCallback;

            public CompassListener(Action<SensorEvent> sensorChangedCallback)
            {
                _sensorChangedCallback = sensorChangedCallback;
            }

            public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
            {
            }

            public void OnSensorChanged(SensorEvent e)
            {
                _sensorChangedCallback(e);
            }
        }

        private SensorManager _sensorManager;
        private Sensor _compassSensor;
        private CompassListener _compassListener;

        protected override string DisplayName
        {
            get { return "Compass"; }
        }

        public AndroidCompassProbe()
        {
            _compassListener = new CompassListener(e =>
                {
                    StoreDatum(new CompassDatum(Id, new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0)), e.Values[0]));
                });
        }

        protected override bool Initialize()
        {
            base.Initialize();

            _sensorManager = (App.Get() as AndroidApp).Context.GetSystemService(Context.SensorService) as SensorManager;
            IList<Sensor> orientationSensors = _sensorManager.GetSensorList(SensorType.Orientation);
            if (orientationSensors.Count > 0)
                _compassSensor = orientationSensors[0];
            else
                Supported = false;

            return Supported;
        }

        public override void StartListening()
        {
            _sensorManager.RegisterListener(_compassListener, _compassSensor, SensorDelay.Ui);
        }

        public override void StopListening()
        {
            _sensorManager.UnregisterListener(_compassListener);
        }
    }
}