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