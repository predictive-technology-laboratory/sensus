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

        private static string _previousApBSSID = null;
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
                string apBSSID = GetAccessPointBSSID();
                if (_firstReceive || apBSSID != _previousApBSSID)
                {
                    WifiConnectionChanged(this, new WlanDatum(null, DateTimeOffset.UtcNow, apBSSID));
                    _previousApBSSID = apBSSID;
                    _firstReceive = false;
                }
            }
        }

        private string GetAccessPointBSSID()
        {
            string apBSSID = null;

            if (_connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).IsConnected)
            {
                WifiManager wifiManager = Application.Context.GetSystemService(global::Android.Content.Context.WifiService) as WifiManager;
                apBSSID = wifiManager.ConnectionInfo.BSSID;
            }

            return apBSSID;
        }
    }
}