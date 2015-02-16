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
using SensusService.Probes.Network;
using System;

namespace Sensus.Android.Probes.Network
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { ConnectivityManager.ConnectivityAction }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidWlanBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<WlanDatum> WifiConnectionChanged;

        private static string _previousAccessPointBSSID = null;
        private static bool _firstReceive = true;

        private ConnectivityManager _connectivityManager;

        public AndroidWlanBroadcastReceiver()
        {
            _connectivityManager = Application.Context.GetSystemService(global::Android.Content.Context.ConnectivityService) as ConnectivityManager;
        }

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (WifiConnectionChanged != null && intent != null && intent.Action == ConnectivityManager.ConnectivityAction)
            {
                string currAccessPointBSSID = GetAccessPointBSSID();
                if (_firstReceive || currAccessPointBSSID != _previousAccessPointBSSID)
                {
                    WifiConnectionChanged(this, new WlanDatum(null, DateTimeOffset.UtcNow, currAccessPointBSSID));
                    _previousAccessPointBSSID = currAccessPointBSSID;
                    _firstReceive = false;
                }
            }
        }

        private string GetAccessPointBSSID()
        {
            string accessPointBSSID = null;

            if (_connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).IsConnected)
            {
                WifiManager wifiManager = Application.Context.GetSystemService(global::Android.Content.Context.WifiService) as WifiManager;
                accessPointBSSID = wifiManager.ConnectionInfo.BSSID;
            }

            return accessPointBSSID;
        }
    }
}
