using Android.App;
using Android.Content;
using Android.OS;
using Android.Telephony;
using System;
using System.Collections.Generic;

namespace Sensus.Android.Probes
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { "android.provider.Telephony.SMS_SENT", "android.provider.Telephony.SMS_RECEIVED" }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidSmsBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<SmsMessage> MessageSent;
        public static event EventHandler<SmsMessage> MessageReceived;

        public static void Stop()
        {
            MessageSent = null;
            MessageReceived = null;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            List<SmsMessage> messages = new List<SmsMessage>();

            Bundle bundle = intent.Extras;
            if (bundle != null)
            {
                try
                {
                    Java.Lang.Object[] pdus = (Java.Lang.Object[])bundle.Get("pdus");
                    for (int i = 0; i < pdus.Length; i++)
                        messages.Add(SmsMessage.CreateFromPdu((byte[])pdus[i]));
                }
                catch (Exception) { }
            }

            if (intent.Action == "android.provider.Telephony.SMS_SENT" && MessageSent != null)
                foreach (SmsMessage message in messages)
                    MessageSent(this, message);
            else if (intent.Action == "android.provider.Telephony.SMS_RECEIVED" && MessageReceived != null)
                foreach (SmsMessage message in messages)
                    MessageReceived(this, message);
        }
    }
}