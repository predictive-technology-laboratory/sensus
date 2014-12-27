using Android.App;
using Android.Content;
using Android.Net.Wifi;
using SensusService.Probes.Network;
using System;

namespace Sensus.Android.Probes.Network
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { WifiManager.SupplicantConnectionChangeAction }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidWlanBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<WlanDatum> WifiConnectionChanged;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (WifiConnectionChanged != null && intent != null && intent.Action == WifiManager.SupplicantConnectionChangeAction)
            {
                bool connected = intent.GetBooleanExtra(WifiManager.ExtraSupplicantConnected, false);
                string bssid = intent.GetStringExtra(WifiManager.ExtraBssid);
                WifiConnectionChanged(this, new WlanDatum(null, DateTimeOffset.UtcNow, connected ? bssid : null));
            }
        }
    }
}