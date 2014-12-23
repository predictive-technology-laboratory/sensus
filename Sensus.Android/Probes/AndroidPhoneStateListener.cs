using Android.Telephony;
using System;

namespace Sensus.Android.Probes
{
    public class AndroidPhoneStateListener : PhoneStateListener
    {
        public class StateChangedEventArgs : EventArgs
        {
            public CallState CallState { get; set; }

            public string IncomingNumber { get; set; }
        }

        public event EventHandler<StateChangedEventArgs> CallStateChanged;
        public event EventHandler<CellLocation> CellLocationChanged;

        public override void OnCallStateChanged(CallState state, string incomingNumber)
        {
            if (CallStateChanged != null)
                CallStateChanged(this, new StateChangedEventArgs { CallState = state, IncomingNumber = incomingNumber });
        }

        public override void OnCellLocationChanged(CellLocation location)
        {
            if (CellLocationChanged != null)
                CellLocationChanged(this, location);
        }
    }
}