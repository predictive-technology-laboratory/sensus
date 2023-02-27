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
using Android.Net.Wifi;
using Sensus.Exceptions;
using Sensus.Probes.Network;
using System;

#warning AndroidWlanBroadcastReceiver has obsolete code.
#pragma warning disable CS0618 // Type or member is obsolete
namespace Sensus.Android.Probes.Network
{
	public class AndroidWlanBroadcastReceiver : BroadcastReceiver
	{
		public static event EventHandler<WlanDatum> WIFI_CONNECTION_CHANGED;

		private static string PREVIOUS_ACCESS_POINT_BSSID = null;
		private static bool FIRST_RECEIVE = true;

		public override void OnReceive(global::Android.Content.Context context, Intent intent)
		{
			// this method is usually called on the UI thread and can crash the app if it throws an exception
			try
			{
				if (intent == null)
				{
					throw new ArgumentNullException(nameof(intent));
				}

				if (intent.Action == ConnectivityManager.ConnectivityAction)
				{
					// we've seen duplicate reports of the BSSID. only call the event handler if this is the first report
					// or if the BSSID has changed to a new value. it's possible for the current to be null multiple times
					// hence the need for both the first check as well as the change check.
					string currAccessPointBSSID = null;
					string currAccessPointRssi = null;

					if (SensusServiceHelper.Get().WiFiConnected)
					{
						WifiManager wifiManager = Application.Context.GetSystemService(global::Android.Content.Context.WifiService) as WifiManager;
						currAccessPointBSSID = wifiManager.ConnectionInfo.BSSID;
						currAccessPointRssi = wifiManager.ConnectionInfo.Rssi.ToString();

					}

					if (FIRST_RECEIVE || currAccessPointBSSID != PREVIOUS_ACCESS_POINT_BSSID)
					{
						WIFI_CONNECTION_CHANGED?.Invoke(this, new WlanDatum(DateTimeOffset.UtcNow, currAccessPointBSSID, currAccessPointRssi));
						PREVIOUS_ACCESS_POINT_BSSID = currAccessPointBSSID;
						FIRST_RECEIVE = false;
					}
				}
			}
			catch (Exception ex)
			{
				SensusException.Report("Exception in WLAN broadcast receiver:  " + ex.Message, ex);
			}
		}
	}
}
#pragma warning restore CS0618 // Type or member is obsolete