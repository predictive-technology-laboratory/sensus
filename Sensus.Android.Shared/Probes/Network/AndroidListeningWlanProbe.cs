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

using Android.App;
using Android.Content;
using Android.Net;
using Sensus.Probes.Network;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Sensus.Android.Probes.Network
{
	public class AndroidListeningWlanProbe : ListeningWlanProbe
	{
		private readonly AndroidNetworkCallback _callback;

		public AndroidListeningWlanProbe()
		{
			_callback = new (this);
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync<Permissions.NetworkState>() == PermissionStatus.Granted)
			{
				if (AndroidSensusServiceHelper.ConnectivityManager == null)
				{
					throw new NotSupportedException("No network present.");
				}
			}
			else
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Checking network status is not permitted on this device. Cannot start WLAN probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}
		}

		public async Task CreateDatumAsync(string bssid, int rssi)
		{
			await StoreDatumAsync(new WlanDatum(DateTimeOffset.UtcNow, bssid, rssi.ToString()));
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			NetworkRequest.Builder builder = new();

			builder.AddCapability(NetCapability.Internet);
			builder.AddTransportType(TransportType.Wifi);

			NetworkRequest request = builder.Build();

			AndroidSensusServiceHelper.ConnectivityManager.RegisterNetworkCallback(request, _callback);
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			AndroidSensusServiceHelper.ConnectivityManager.UnregisterNetworkCallback(_callback);
		}
	}
}
