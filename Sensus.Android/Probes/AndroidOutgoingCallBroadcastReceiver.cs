using Android.App;
using Android.Content;
using System;

namespace Sensus.Android.Probes
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionNewOutgoingCall }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidOutgoingCallBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<string> CallMade;
        public static object _staticLockObject = new object();

        public static void Stop()
        {
            lock (_staticLockObject)
                CallMade = null;
        }

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            lock (this)
                if (intent != null && intent.Action == Intent.ActionNewOutgoingCall && CallMade != null)
                    CallMade(this, intent.GetStringExtra(Intent.ExtraPhoneNumber));
        }
    }
}