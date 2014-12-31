#region copyright
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
#endregion
 
using Android.App;
using Android.Content;
using Android.OS;
using Android.Telephony;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { "android.provider.Telephony.SMS_RECEIVED" }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidSmsIncomingBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<SmsDatum> IncomingSMS;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (IncomingSMS != null && intent != null && intent.Action == "android.provider.Telephony.SMS_RECEIVED")
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
                            IncomingSMS(this, new SmsDatum(null, timestamp, message.OriginatingAddress, null, message.MessageBody));
                        }
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}