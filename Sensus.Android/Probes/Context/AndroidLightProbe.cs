using Android.Hardware;
using SensusService.Probes.Context;
using System;

namespace Sensus.Android.Probes.Context
{
    public class AndroidLightProbe : LightProbe
    {
        private AndroidSensorListener _lightListener;

        public AndroidLightProbe()
        {
            _lightListener = new AndroidSensorListener(SensorType.Light, SensorDelay.Normal, null, e =>
                {
                    StoreDatum(new LightDatum(this, DateTimeOffset.UtcNow, e.Values[0]));
                });
        }

        protected override bool Initialize()
        {
            return base.Initialize() && _lightListener.Initialize();
        }

        public override void StartListening()
        {
            _lightListener.Start();
        }

        public override void StopListening()
        {
            _lightListener.Stop();
        }
    }
}