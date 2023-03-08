﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using System.Threading.Tasks;
using Xamarin.Essentials;

#warning AndroidCellTowerProbe uses obsolete code.
#pragma warning disable CS0618 // Type or member is obsolete
namespace Sensus.Android.Probes.Network
{
	public class AndroidCellTowerProbe : CellTowerProbe
	{
		private TelephonyManager _telephonyManager;
		private AndroidCellTowerChangeListener _cellTowerChangeListener;

		public AndroidCellTowerProbe()
		{
			_cellTowerChangeListener = new AndroidCellTowerChangeListener();
			_cellTowerChangeListener.CellTowerChanged += async (o, cellTowerLocation) =>
			{
				await StoreDatumAsync(new CellTowerDatum(DateTimeOffset.UtcNow, cellTowerLocation));
			};
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainLocationPermissionAsync() == PermissionStatus.Granted)
			{
				_telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
				if (_telephonyManager == null)
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

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			_telephonyManager.Listen(_cellTowerChangeListener, PhoneStateListenerFlags.CellLocation);
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			_telephonyManager.Listen(_cellTowerChangeListener, PhoneStateListenerFlags.None);
		}
	}
}
#pragma warning restore CS0618 // Type or member is obsolete
