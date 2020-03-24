using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.Hardware;
using Sensus.Probes.Movement;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidLinearAccelerationProbe : LinearAccelerationProbe
    {
        private AndroidSensorListener _linearaccelerationListener;

        public AndroidLinearAccelerationProbe()
        {
            _linearaccelerationListener = new AndroidSensorListener(SensorType.LinearAcceleration, async e =>
            {
                // should get x, y, and z values
                if (e.Values.Count != 3)
                {
                    return;
                }

                  await StoreDatumAsync(new LinearAccelerationDatum(DateTimeOffset.UtcNow, e.Values[0], e.Values[1], e.Values[2] ));
            });
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _linearaccelerationListener.Initialize(MinDataStoreDelay);
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _linearaccelerationListener.Start();
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();

            _linearaccelerationListener.Stop();
        }
    }
}
