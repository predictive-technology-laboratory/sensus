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

namespace Sensus.Android.Probes.Network
{
    public class AndroidListeningWlanProbe : ListeningWlanProbe
    {
        private AndroidWlanBroadcastReceiver _wlanBroadcastReceiver;
        private EventHandler<WlanDatum> _wlanConnectionChangedCallback;

        public AndroidListeningWlanProbe()
        {
            _wlanBroadcastReceiver = new AndroidWlanBroadcastReceiver();

            _wlanConnectionChangedCallback = async (sender, wlanDatum) =>
            {
                await StoreDatumAsync(wlanDatum);
            };
        }

        protected override Task StartListeningAsync()
        {
            // register receiver for all WLAN intent actions
            Application.Context.RegisterReceiver(_wlanBroadcastReceiver, new IntentFilter(ConnectivityManager.ConnectivityAction));

            AndroidWlanBroadcastReceiver.WIFI_CONNECTION_CHANGED += _wlanConnectionChangedCallback;

            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            // stop broadcast receiver
            Application.Context.UnregisterReceiver(_wlanBroadcastReceiver);

            AndroidWlanBroadcastReceiver.WIFI_CONNECTION_CHANGED -= _wlanConnectionChangedCallback;

            return Task.CompletedTask;
        }
    }
}