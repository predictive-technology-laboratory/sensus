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
