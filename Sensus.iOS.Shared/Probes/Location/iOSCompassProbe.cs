// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Sensus.Probes.Location;
using Plugin.Geolocator.Abstractions;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Sensus.iOS.Probes.Location
{
	public class iOSCompassProbe : CompassProbe
	{
		private EventHandler<PositionEventArgs> _positionChangedHandler;

		/// <summary>
		/// This <see cref="Sensus.Probes.Probe"/> uses continuous GPS listening and will have a significant negative impact on battery life.
		/// </summary>
		protected override bool WillHaveSignificantNegativeImpactOnBattery => true;

		public iOSCompassProbe()
		{
			_positionChangedHandler = async (o, e) =>
			{
				SensusServiceHelper.Get().Logger.Log("Received compass change notification.", LoggingLevel.Verbose, GetType());
				await StoreDatumAsync(new CompassDatum(e.Position.Timestamp, e.Position.Heading));
			};
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainLocationPermissionAsync() != PermissionStatus.Granted)
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Geolocation / heading are not permitted on this device. Cannot start compass probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			await GpsReceiver.Get().AddListenerAsync(_positionChangedHandler, true);
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			await GpsReceiver.Get().RemoveListenerAsync(_positionChangedHandler);
		}

		protected override ChartSeries GetChartSeries()
		{
			throw new NotImplementedException();
		}

		protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
		{
			throw new NotImplementedException();
		}

		protected override ChartAxis GetChartPrimaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override RangeAxisBase GetChartSecondaryAxis()
		{
			throw new NotImplementedException();
		}
	}
}

