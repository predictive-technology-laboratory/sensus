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

namespace Sensus.Android.Probes.Communication
{
    /// <summary>
    /// Listens for new incoming call and idle states of the phone. Since it is not easy to differentiate incoming
    /// from outgoing calls on the basis of <see cref="CallState"/> values, we also need the <see cref="AndroidTelephonyOutgoingBroadcastReceiver"/>.
    /// The difficulty is caused by the fact that an incoming call will transition to <see cref="CallState.Ringing"/> and then immediately to
    /// <see cref="CallState.Offhook"/>, whereas an outgoing call will transition directly to <see cref="CallState.Offhook"/>. We'd rather not have to interpret 
    /// <see cref="CallState.Offhook"/> differently depending on what the previous state was and how recently it was observed. 
    /// </summary>
    public class AndroidTelephonyIdleIncomingListener : PhoneStateListener
    {
        public event EventHandler<string> Idle;
        public event EventHandler<string> IncomingCall;

        public override void OnCallStateChanged(CallState state, string phoneNumber)
        {
            if (state == CallState.Idle)
            {
                Idle?.Invoke(this, phoneNumber);
            }
            else if (state == CallState.Ringing)
            {
                IncomingCall?.Invoke(this, phoneNumber);
            }
        }
    }
}