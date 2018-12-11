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
using Android.OS;
using Android.Telephony;
using Sensus.Exceptions;
using Sensus.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { "android.provider.Telephony.SMS_RECEIVED" }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidSmsIncomingBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<SmsDatum> INCOMING_SMS;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

                if (INCOMING_SMS != null && intent.Action == "android.provider.Telephony.SMS_RECEIVED")
                {
                    Bundle bundle = intent.Extras;

                    if (bundle != null)
                    {
                        Java.Lang.Object[] pdus = (Java.Lang.Object[])bundle.Get("pdus");
                        for (int i = 0; i < pdus.Length; i++)
                        {
                            SmsMessage message;

                            // see the Backwards Compatibility article for more information
#if __ANDROID_23__
                            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                            {
                                message = SmsMessage.CreateFromPdu((byte[])pdus[i], intent.GetStringExtra("format"));  // API 23
                            }
                            else
#endif
                            {
                                // ignore deprecation warning
#pragma warning disable 618
                                message = SmsMessage.CreateFromPdu((byte[])pdus[i]);
#pragma warning restore 618
                            }

                            INCOMING_SMS(this, new SmsDatum(DateTimeOffset.FromUnixTimeMilliseconds(message.TimestampMillis), message.OriginatingAddress, null, message.MessageBody, false));
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception in SMS broadcast receiver:  " + ex.Message, ex);
            }
        }
    }
}
