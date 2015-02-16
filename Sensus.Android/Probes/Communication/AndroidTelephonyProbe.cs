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
    public class AndroidTelephonyProbe : TelephonyProbe
    {
        private TelephonyManager _telephonyManager;
        private EventHandler<string> _outgoingCallCallback;
        private AndroidTelephonyIdleIncomingListener _idleIncomingCallListener;

        public AndroidTelephonyProbe()
        {
            _outgoingCallCallback = (sender, outgoingNumber) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, TelephonyState.OutgoingCall, outgoingNumber));
                };

            _idleIncomingCallListener = new AndroidTelephonyIdleIncomingListener();

            _idleIncomingCallListener.IncomingCall += (o, incomingNumber) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, TelephonyState.IncomingCall, incomingNumber));
                };

            _idleIncomingCallListener.Idle += (o, e) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, TelephonyState.Idle, null));
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
            AndroidTelephonyOutgoingBroadcastReceiver.OutgoingCall += _outgoingCallCallback;
            _telephonyManager.Listen(_idleIncomingCallListener, PhoneStateListenerFlags.CallState);
        }

        protected override void StopListening()
        {
            AndroidTelephonyOutgoingBroadcastReceiver.OutgoingCall -= _outgoingCallCallback;
            _telephonyManager.Listen(_idleIncomingCallListener, PhoneStateListenerFlags.None);
        }
    }
}
