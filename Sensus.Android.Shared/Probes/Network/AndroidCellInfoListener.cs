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
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Telephony;
using Android.Telephony.Cdma;
using Android.Telephony.Gsm;

namespace Sensus.Android.Probes.Network
{
	public class AndroidCellInfoListener
	{
		private readonly AndroidCellTowerProbe _probe;
		private readonly AndroidCellInfoCallback _callback;
		private readonly AndroidCellLocationListenerShim _listener;

		public AndroidCellInfoListener(AndroidCellTowerProbe probe)
		{
			_probe = probe;

			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			{
				_callback = new AndroidCellInfoCallback(this);
			}
			else
			{
				_listener = new AndroidCellLocationListenerShim(this);
			}
		}

		public class AndroidCellInfoCallback : TelephonyCallback, TelephonyCallback.ICellInfoListener
		{
			private readonly AndroidCellInfoListener _listener;

			public AndroidCellInfoCallback(AndroidCellInfoListener listener)
			{
				_listener = listener;
			}

			public void OnCellInfoChanged(IList<CellInfo> cellInfo)
			{
				_listener.OnCellInfoChanged(cellInfo);
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class AndroidCellLocationListenerShim : PhoneStateListener
		{
			private readonly AndroidCellInfoListener _listener;

			public AndroidCellLocationListenerShim(AndroidCellInfoListener listener)
			{
				_listener = listener;
			}

			[Obsolete]
			public override void OnCellLocationChanged(CellLocation location)
			{
				_listener.OnCellLocationChanged(location);
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public void StartListening()
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			{
				AndroidSensusServiceHelper.TelephonyManager.RegisterTelephonyCallback(Application.Context.MainExecutor, _callback);
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				AndroidSensusServiceHelper.TelephonyManager.Listen(_listener, PhoneStateListenerFlags.CellLocation);
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public void StopListening()
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			{
				AndroidSensusServiceHelper.TelephonyManager.UnregisterTelephonyCallback(null);
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				AndroidSensusServiceHelper.TelephonyManager.Listen(_listener, PhoneStateListenerFlags.None);
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public async void OnCellInfoChanged(IList<CellInfo> cellInfos)
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
				/*else if (cellInfo is CellInfoLte lte)
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

				}*/

				await _probe.CreateDatumsAsync(DateTimeOffset.FromUnixTimeMilliseconds(cellInfo.TimestampMillis), cellIdentity);
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public async void OnCellLocationChanged(CellLocation location)
		{
			string cellTowerInformation = null;

			if (location is CdmaCellLocation)
			{
				CdmaCellLocation cdmaLocation = location as CdmaCellLocation;

				cellTowerInformation = $"[base_station_id={cdmaLocation.BaseStationId},base_station_lat={cdmaLocation.BaseStationLatitude},base_station_lon={cdmaLocation.BaseStationLongitude},network_id={cdmaLocation.NetworkId},system_id={cdmaLocation.SystemId}]";
			}
			else if (location is GsmCellLocation)
			{
				GsmCellLocation gsmLocation = location as GsmCellLocation;

				cellTowerInformation = $"[cid={gsmLocation.Cid},lac={gsmLocation.Lac}]";
			}
			else
			{
				cellTowerInformation = location?.ToString();
			}

			await _probe.CreateDatumsAsync(DateTimeOffset.UtcNow, cellTowerInformation);
		}
#pragma warning restore CS0618 // Type or member is obsolete
	}
}
