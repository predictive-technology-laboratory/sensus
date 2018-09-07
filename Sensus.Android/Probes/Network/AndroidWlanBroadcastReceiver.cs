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
using Sensus.Probes.Network;
using System;

namespace Sensus.Android.Probes.Network
{
    public class AndroidWlanBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<WlanDatum> WIFI_CONNECTION_CHANGED;

        private static string PREVIOUS_ACCESS_POINT_BSSID = null;
        private static bool FIRST_RECEIVE = true;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (WIFI_CONNECTION_CHANGED != null && intent != null && intent.Action == ConnectivityManager.ConnectivityAction)
            {
                string currAccessPointBSSID = GetAccessPointBSSID();
                if (FIRST_RECEIVE || currAccessPointBSSID != PREVIOUS_ACCESS_POINT_BSSID)
                {
                    WIFI_CONNECTION_CHANGED(this, new WlanDatum(DateTimeOffset.UtcNow, currAccessPointBSSID));
                    PREVIOUS_ACCESS_POINT_BSSID = currAccessPointBSSID;
                    FIRST_RECEIVE = false;
                }
            }
        }

        private string GetAccessPointBSSID()
        {
            string accessPointBSSID = null;

            if (SensusServiceHelper.Get().WiFiConnected)
            {
                WifiManager wifiManager = Application.Context.GetSystemService(global::Android.Content.Context.WifiService) as WifiManager;
                accessPointBSSID = wifiManager.ConnectionInfo.BSSID;
            }

            return accessPointBSSID;
        }
    }
}
