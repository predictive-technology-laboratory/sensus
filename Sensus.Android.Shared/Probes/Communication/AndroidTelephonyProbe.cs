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
using Sensus.Probes.Communication;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Sensus.Android.Probes.Communication
{
	public class AndroidTelephonyProbe : ListeningTelephonyProbe
	{
		private readonly AndroidCallStateListener _listener;

		public AndroidTelephonyProbe()
		{
			_listener = new(this);
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync<Permissions.Phone>() == PermissionStatus.Granted)
			{
				if (AndroidSensusServiceHelper.TelephonyManager == null)
				{
					throw new NotSupportedException("No telephony present.");
				}
			}
			else
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable phone in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Telephony not permitted on this device. Cannot start telephony probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}
		}

		public async Task CreateDatumAsync(int state)
		{
			await StoreDatumAsync(new TelephonyDatum(DateTimeOffset.UtcNow, (TelephonyState)state));
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			AndroidSensusServiceHelper.TelephonyManager.RegisterTelephonyCallback(Application.Context.MainExecutor, _listener);
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			AndroidSensusServiceHelper.TelephonyManager.UnregisterTelephonyCallback(_listener);
		}
	}
}
