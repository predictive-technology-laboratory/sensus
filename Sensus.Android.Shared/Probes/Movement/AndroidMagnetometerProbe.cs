using System;
using System.Threading.Tasks;
using Android.Hardware;
using Sensus.Probes.Movement;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidMagnetometerProbe : MagnetometerProbe
    {

        private AndroidSensorListener _magnetometerListener;

        public AndroidMagnetometerProbe()
        {

            _magnetometerListener = new AndroidSensorListener(SensorType.MagneticField, async e =>
            {
                // should get geomagnetic strength along x, y, and z values
                if (e.Values.Count != 3)
                {
                    return;
                }
                await StoreDatumAsync(new MagnetometerDatum(DateTimeOffset.UtcNow, e.Values[0], e.Values[1], e.Values[2]));
            });


        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            _magnetometerListener.Initialize(MinDataStoreDelay);
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _magnetometerListener.Start();
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();

            _magnetometerListener.Stop();
        }
    }
}
