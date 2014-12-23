using Android.App;
using Android.Content;
using Android.Telephony;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidTelephonyProbe : TelephonyProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidPhoneStateListener _phoneStateListener;

        public AndroidTelephonyProbe()
        {
            _telephonyManager = Application.Context.GetSystemService(Context.TelephonyService) as TelephonyManager;

            _phoneStateListener = new AndroidPhoneStateListener();

            _phoneStateListener.CellLocationChanged += (o, e) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, "Cell location:  " + e, null));
                };

            _phoneStateListener.CallStateChanged += (o, e) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, e.CallState.ToString(), e.IncomingNumber));
                };
        }

        public override void StartListening()
        {
            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.CallForwardingIndicator |
                                                          PhoneStateListenerFlags.CallState |
                                                          PhoneStateListenerFlags.CellLocation);
        }

        public override void StopListening()
        {
            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.None);
        }
    }
}