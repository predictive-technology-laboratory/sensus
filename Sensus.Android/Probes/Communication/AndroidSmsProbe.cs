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
using Android.Telephony;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsProbe : SmsProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidSmsOutgoingObserver _smsOutgoingObserver;
        private EventHandler<SmsDatum> _incomingSmsCallback;

        public AndroidSmsProbe()
        {
            _smsOutgoingObserver = new AndroidSmsOutgoingObserver(Application.Context, outgoingSmsDatum =>
                {
                    // the observer doesn't set the from number
                    outgoingSmsDatum.FromNumber = _telephonyManager.Line1Number;

                    StoreDatum(outgoingSmsDatum);
                });

            _incomingSmsCallback = (sender, incomingSmsDatum) =>
                {
                    // the observer doesn't set the destination number (simply the device's primary number)
                    incomingSmsDatum.ToNumber = _telephonyManager.Line1Number;

                    StoreDatum(incomingSmsDatum);
                };
        }

        protected override void Initialize()
        {
            base.Initialize();

            _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
            if (_telephonyManager == null)
                throw new Exception("No telephony present.");
        }

        protected override void StartListening()
        {
            Application.Context.ContentResolver.RegisterContentObserver(global::Android.Net.Uri.Parse("content://sms"), true, _smsOutgoingObserver);
            AndroidSmsIncomingBroadcastReceiver.INCOMING_SMS += _incomingSmsCallback;
        }

        protected override void StopListening()
        {
            Application.Context.ContentResolver.UnregisterContentObserver(_smsOutgoingObserver);
            AndroidSmsIncomingBroadcastReceiver.INCOMING_SMS -= _incomingSmsCallback;
        }
    }
}
