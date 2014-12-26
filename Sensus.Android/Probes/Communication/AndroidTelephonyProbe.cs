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
            _telephonyManager = Application.Context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
            _phoneStateListener = new AndroidPhoneStateListener();

            _phoneStateListener.CallStateChanged += (o, e) =>
                {
                    if (e.CallState == CallState.Ringing)
                        StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, e.CallState.ToString(), e.IncomingNumber));
                };
        }

        public override void StartListening()
        {
            AndroidOutgoingCallBroadcastReceiver.CallMade += (o, phoneNumber) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, CallState.Offhook.ToString(), phoneNumber));
                };

            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.CallState);
        }

        public override void StopListening()
        {
            AndroidOutgoingCallBroadcastReceiver.Stop();
            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.None);
        }
    }
}