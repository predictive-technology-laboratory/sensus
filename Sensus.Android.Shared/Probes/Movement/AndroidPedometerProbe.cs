using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.Hardware;
using Sensus.Probes.Movement;

namespace Sensus.Android.Probes.Movement
{
    class AndroidPedometerProbe : PedometerProbe
    {
        private AndroidSensorListener _pedometerListener;
        public AndroidPedometerProbe()
        {

            _pedometerListener = new AndroidSensorListener(SensorType.StepCounter, async e =>
            {

                await StoreDatumAsync(new PedometerDatum(DateTimeOffset.UtcNow, e.Values[0] ));
            });
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            _pedometerListener.Initialize(MinDataStoreDelay);
        }
        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _pedometerListener.Start();
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();
            _pedometerListener.Stop();

        }
    }
}
