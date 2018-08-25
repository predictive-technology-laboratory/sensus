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

using System;
using Android.App;
using Android.Telephony;
using Sensus;
using Sensus.Probes.Communication;
using Plugin.Permissions.Abstractions;

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
                // the observer doesn't set the from number (current device)
                outgoingSmsDatum.FromNumber = _telephonyManager.Line1Number;

                StoreDatumAsync(outgoingSmsDatum);
            });

            _incomingSmsCallback = (sender, incomingSmsDatum) =>
            {
                // the observer doesn't set the destination number (simply the device's primary number)
                incomingSmsDatum.ToNumber = _telephonyManager.Line1Number;

                StoreDatumAsync(incomingSmsDatum);
            };
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Sms) == PermissionStatus.Granted)
            {
                _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
                if (_telephonyManager == null)
                {
                    throw new NotSupportedException("No telephony present.");
                }
            }
            else
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable SMS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "SMS is not permitted on this device. Cannot start SMS probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
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