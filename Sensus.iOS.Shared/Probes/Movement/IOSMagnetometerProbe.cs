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
	public class iOSMagnetometerProbe : MagnetometerProbe
	{
		private CMMotionManager _motionManager;

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync<Permissions.Sensors>() == PermissionStatus.Granted)
			{
				_motionManager = new CMMotionManager();
			}
			else
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable sensors in the future
				// and we'd like the probe to be restarted at that time.
				string error = "This device does not contain a Magnetometer, or the user has denied access to it. Cannot start magnetometer probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}

		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			_motionManager?.StartMagnetometerUpdates(new NSOperationQueue(), async (data, error) =>
			{
				if (data != null && error == null)
				{
					await StoreDatumAsync(new MagnetometerDatum(DateTimeOffset.UtcNow, data.MagneticField.X, data.MagneticField.Y, data.MagneticField.Z));
				}
			});


		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			_motionManager?.StopDeviceMotionUpdates();


		}
	}
}
