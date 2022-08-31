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
		private readonly AndroidSensorListener _pedometerListener;

		private float _lastValue;

		public AndroidPedometerProbe()
		{
			_lastValue = 0;

			_pedometerListener = new AndroidSensorListener(SensorType.StepCounter, async e =>
			{
				if (_lastValue > 0)
				{
					await StoreDatumAsync(new PedometerDatum(DateTimeOffset.UtcNow, e.Values[0] - _lastValue));
				}

				_lastValue = e.Values[0];
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
