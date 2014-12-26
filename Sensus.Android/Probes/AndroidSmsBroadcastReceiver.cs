using Android.App;
using Android.Content;
using Android.OS;
using Android.Telephony;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { "android.provider.Telephony.SMS_RECEIVED" }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidSmsBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<SmsDatum> MessageReceived;
        public static object _staticLockObject = new object();

        public static void Stop()
        {
            lock (_staticLockObject)
                MessageReceived = null;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            lock (_staticLockObject)
                if (intent != null && intent.Action == "android.provider.Telephony.SMS_RECEIVED" && MessageReceived != null)
                {
                    Bundle bundle = intent.Extras;
                    if (bundle != null)
                    {
                        try
                        {
                            Java.Lang.Object[] pdus = (Java.Lang.Object[])bundle.Get("pdus");
                            for (int i = 0; i < pdus.Length; i++)
                            {
                                SmsMessage message = SmsMessage.CreateFromPdu((byte[])pdus[i]);
                                DateTimeOffset timestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(message.TimestampMillis);
                                MessageReceived(this, new SmsDatum(null, timestamp, message.OriginatingAddress, null, message.MessageBody));
                            }
                        }
                        catch (Exception) { }
                    }
                }
        }
    }
}