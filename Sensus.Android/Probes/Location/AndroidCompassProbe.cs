using Android.Hardware;
using SensusService.Probes.Location;
using System;

namespace Sensus.Android.Probes.Location
{
    public class AndroidCompassProbe : CompassProbe
    {
        private AndroidSensorListener _compassListener;

        public AndroidCompassProbe()
        {
            _compassListener = new AndroidSensorListener(SensorType.Orientation, SensorDelay.Normal, null, e =>
                {
                    StoreDatum(new CompassDatum(this, DateTimeOffset.UtcNow, e.Values[0]));
                });
        }

        protected override bool Initialize()
        {
            return base.Initialize() && _compassListener.Initialize();
        }

        public override void StartListening()
        {
            _compassListener.Start();
        }

        public override void StopListening()
        {
            _compassListener.Stop();
        }
    }
}