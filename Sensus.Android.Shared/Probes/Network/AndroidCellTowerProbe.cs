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

using Sensus.Probes.Network;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Sensus.Android.Probes.Network
{
	public class AndroidCellTowerProbe : CellTowerProbe
	{
		private readonly AndroidCellInfoListener _listener;

		public AndroidCellTowerProbe()
		{
			_listener = new(this);
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainLocationPermissionAsync() == PermissionStatus.Granted)
			{
				if (AndroidSensusServiceHelper.TelephonyManager == null)
				{
					throw new NotSupportedException("No telephony present.");
				}
			}
			else
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Cell tower location is not permitted on this device. Cannot start cell tower probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}
		}

		public async Task CreateDatumsAsync(DateTimeOffset timestamp, string cellInfo)
		{
			await StoreDatumAsync(new CellTowerDatum(timestamp, cellInfo));
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			_listener.StartListening();
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			_listener.StopListening();
		}
	}
}
