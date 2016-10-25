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

using Android.Telephony;
using System;

namespace Sensus.Shared.Android.Probes.Communication
{
    public class AndroidTelephonyIdleIncomingListener : PhoneStateListener
    {
        public event EventHandler Idle;
        public event EventHandler<string> IncomingCall;

        public override void OnCallStateChanged(CallState state, string incomingNumber)
        {
            if (Idle != null && state == CallState.Idle)
                Idle(this, null);
            else if (IncomingCall != null && state == CallState.Ringing)
                IncomingCall(this, incomingNumber);
        }
    }
}
