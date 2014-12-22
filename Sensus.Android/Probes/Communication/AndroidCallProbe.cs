using Android.App;
using Android.Content;
using Android.Telephony;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidCallProbe : CallProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidPhoneStateListener _phoneStateListener;

        public AndroidCallProbe()
        {
            _telephonyManager = Application.Context.GetSystemService(Context.TelephonyService) as TelephonyManager;

            _phoneStateListener = new AndroidPhoneStateListener();
            _phoneStateListener.CallStateChanged += (o, e) =>
                {
                    bool incoming = false;
                    string number = null;

                    if (e.CallState == CallState.Ringing)
                    {
                        incoming = true;
                        number = e.IncomingNumber;
                    }

                    StoreDatum(new CallDatum(this, DateTimeOffset.UtcNow, incoming, number));
                };
        }

        public override void StartListening()
        {
            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.CallState);
        }

        public override void StopListening()
        {
            _telephonyManager.Listen(_phoneStateListener, PhoneStateListenerFlags.None);
        }
    }
}