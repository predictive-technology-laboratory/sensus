using Android.Telephony;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidTelephonyIncomingListener : PhoneStateListener
    {
        public event EventHandler<string> IncomingCall;

        public override void OnCallStateChanged(CallState state, string incomingNumber)
        {
            if (IncomingCall != null && state == CallState.Ringing)
                IncomingCall(this, incomingNumber);
        }
    }
}