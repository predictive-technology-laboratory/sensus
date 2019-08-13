using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.Hardware;
using Sensus.Probes.Movement;

namespace Sensus.Android.Probes.Movement
{
    class AndroidAttitudeProbe : AttitudeProbe
    {

        private AndroidSensorListener _attitudeListener;
        public AndroidAttitudeProbe()
        {

            _attitudeListener = new AndroidSensorListener(SensorType.RotationVector, async e =>
            {
                // should get x, y,z, w values
               
                await StoreDatumAsync(new AttitudeDatum(DateTimeOffset.UtcNow, e.Values[0], e.Values[1], e.Values[2], e.Values[3]));
            });


        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            _attitudeListener.Initialize(MinDataStoreDelay);
        }

        protected override  async Task StartListeningAsync()
        {
            await base.StartListeningAsync();
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();
        }
    }
}
