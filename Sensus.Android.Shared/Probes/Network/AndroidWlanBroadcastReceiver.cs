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
using Android.Net.Wifi;
using Sensus.Exceptions;
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
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

#pragma warning disable CS0618 // Type or member is obsolete
                if (intent.Action == ConnectivityManager.ConnectivityAction)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    // we've seen duplicate reports of the BSSID. only call the event handler if this is the first report
                    // or if the BSSID has changed to a new value. it's possible for the current to be null multiple times
                    // hence the need for both the first check as well as the change check.
                    string currAccessPointBSSID = GetAccessPointBSSID();
                    if (FIRST_RECEIVE || currAccessPointBSSID != PREVIOUS_ACCESS_POINT_BSSID)
                    {
                        WIFI_CONNECTION_CHANGED?.Invoke(this, new WlanDatum(DateTimeOffset.UtcNow, currAccessPointBSSID));
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
