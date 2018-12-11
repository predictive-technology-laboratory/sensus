//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
#pragma warning disable CS0618 // Type or member is obsolete
            Application.Context.RegisterReceiver(_wlanBroadcastReceiver, new IntentFilter(ConnectivityManager.ConnectivityAction));
#pragma warning restore CS0618 // Type or member is obsolete

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
