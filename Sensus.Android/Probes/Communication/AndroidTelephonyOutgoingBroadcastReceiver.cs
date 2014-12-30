using Android.App;
using Android.Content;
using System;

namespace Sensus.Android.Probes.Communication
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionNewOutgoingCall }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidTelephonyOutgoingBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<string> OutgoingCall;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (OutgoingCall != null && intent != null && intent.Action == Intent.ActionNewOutgoingCall)
                OutgoingCall(this, intent.GetStringExtra(Intent.ExtraPhoneNumber));
        }
    }
}