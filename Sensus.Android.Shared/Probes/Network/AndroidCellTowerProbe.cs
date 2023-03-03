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
using Android.Telephony;
using Sensus.Probes.Network;
using System;
using System.Collections.Generic;
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

		public async Task CreateDatumsAsync(IList<CellInfo> cellInfos)
		{
			foreach (CellInfo cellInfo in cellInfos)
			{
				string cellIdentity = cellInfo.CellIdentity.ToString();

				if (cellInfo is CellInfoCdma cdma)
				{
					cellIdentity = $"[base_station_id={cdma.CellIdentity.BasestationId},base_station_lat={cdma.CellIdentity.Latitude},base_station_lon={cdma.CellIdentity.Longitude},network_id={cdma.CellIdentity.NetworkId},system_id={cdma.CellIdentity.SystemId}]";
				}
				else if (cellInfo is CellInfoGsm gsm)
				{
					cellIdentity = $"[cid={gsm.CellIdentity.Cid},lac={gsm.CellIdentity.Lac}]";
				}
				else if (cellInfo is CellInfoLte lte)
				{

				}
				else if (cellInfo is CellInfoNr nr)
				{

				}
				else if (cellInfo is CellInfoTdscdma tdscdma)
				{

				}
				else if (cellInfo is CellInfoWcdma wcdma)
				{

				}

				await StoreDatumAsync(new CellTowerDatum(DateTimeOffset.FromUnixTimeMilliseconds(cellInfo.TimestampMillis), cellIdentity));
			}
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
