using Android.App;
using Android.Content;
using Android.Telephony;
using SensusService;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsProbe : SmsProbe
    {
        private TelephonyManager _telephonyManager;
        private AndroidSmsSendObserver _smsSendObserver;

        public AndroidSmsProbe()
        {
            _telephonyManager = Application.Context.GetSystemService(Context.TelephonyService) as TelephonyManager;
            _smsSendObserver = new AndroidSmsSendObserver(Application.Context);
        }

        protected override bool Initialize()
        {
            try
            {
                _smsSendObserver = new AndroidSmsSendObserver(Application.Context);
                Application.Context.ContentResolver.RegisterContentObserver(global::Android.Net.Uri.Parse("content://sms"), true, _smsSendObserver);

                return base.Initialize();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to initialize AndroidSmsProbe:  " + ex.Message, LoggingLevel.Normal);
                return false;
            }
        }

        public override void StartListening()
        {
            _smsSendObserver.MessageSent += (o, d) =>
                {
                    // the observer doesn't set the probe type
                    d.ProbeType = GetType().FullName;

                    StoreDatum(d);
                };

            AndroidSmsBroadcastReceiver.MessageReceived += (o, d) =>
                {
                    // the observer doesn't set the probe type or destination number (simply the device's primary number)
                    d.ProbeType = GetType().FullName;
                    d.ToNumber = _telephonyManager.Line1Number;

                    StoreDatum(d);
                };
        }

        public override void StopListening()
        {
            _smsSendObserver.Stop();
            AndroidSmsBroadcastReceiver.Stop();
        }
    }
}