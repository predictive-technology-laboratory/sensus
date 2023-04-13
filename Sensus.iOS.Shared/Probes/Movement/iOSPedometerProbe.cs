using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CoreMotion;
using Foundation;

using Sensus.Probes.Movement;
using Xamarin.Essentials;

namespace Sensus.iOS.Probes.Movement
{
    class iOSPedometerProbe : PedometerProbe
    {
        private CMPedometer _motionManager;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync<Permissions.Sensors>() == PermissionStatus.Granted)
            {
                _motionManager = new CMPedometer();
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable sensors in the future
                // and we'd like the probe to be restarted at that time.
                string error = "This device does not contain a Pedometer, or the user has denied access to it. Cannot start pedometer probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            _motionManager?.StartPedometerUpdates (new NSDate(), async (data, error) =>
            {
                if (data != null && error == null)
                {

                    await StoreDatumAsync(new PedometerDatum(DateTimeOffset.UtcNow, (double) data.NumberOfSteps));
                }
            });
        }

        protected override async Task StopListeningAsync()
        {
            await base.StopListeningAsync();
            _motionManager?.StopPedometerUpdates();
        }
    }
}
