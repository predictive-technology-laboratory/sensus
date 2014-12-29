using Android.App;
using Android.Telephony;
using SensusService;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidTelephonyProbe : TelephonyProbe
    {
        private TelephonyManager _telephonyManager;
        private EventHandler<string> _outgoingCallCallback;
        private AndroidTelephonyIncomingListener _incomingCallListener;

        public AndroidTelephonyProbe()
        {
            _outgoingCallCallback = (sender, outgoingNumber) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, CallState.Offhook.ToString(), outgoingNumber));
                };

            _incomingCallListener = new AndroidTelephonyIncomingListener();
            _incomingCallListener.IncomingCall += (o, incomingNumber) =>
                {
                    StoreDatum(new TelephonyDatum(this, DateTimeOffset.UtcNow, CallState.Ringing.ToString(), incomingNumber));
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
            _telephonyManager.Listen(_incomingCallListener, PhoneStateListenerFlags.CallState);
        }

        protected override void StopListening()
        {
            AndroidTelephonyOutgoingBroadcastReceiver.OutgoingCall -= _outgoingCallCallback;
            _telephonyManager.Listen(_incomingCallListener, PhoneStateListenerFlags.None);
        }
    }
}