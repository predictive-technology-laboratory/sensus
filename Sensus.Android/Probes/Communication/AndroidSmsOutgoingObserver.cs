using Android.Database;
using Android.Provider;
using Android.Telephony;
using SensusService.Probes;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsOutgoingObserver : ContentObserver
    {
        private Probe _probe;
        private global::Android.Content.Context _context;
        private Action<SmsDatum> _outgoingSMS;
        private TelephonyManager _telephonyManager;

        public AndroidSmsOutgoingObserver(Probe probe, global::Android.Content.Context context, Action<SmsDatum> outgoingSmsCallback)
            : base(null)
        {
            _probe = probe;
            _context = context;
            _outgoingSMS = outgoingSmsCallback;

            _telephonyManager = _context.GetSystemService(global::Android.Content.Context.TelephonyService) as TelephonyManager;
            if (_telephonyManager == null)
                throw new Exception("No telephony present.");
        }

        public override void OnChange(bool selfChange)
        {
            OnChange(selfChange, global::Android.Net.Uri.Parse("content://sms"));
        }

        public override void OnChange(bool selfChange, global::Android.Net.Uri uri)
        {
            ICursor cursor = _context.ContentResolver.Query(uri, null, null, null, null);
            if (cursor.MoveToNext())
            {
                string protocol = cursor.GetString(cursor.GetColumnIndex("protocol"));
                int type = cursor.GetInt(cursor.GetColumnIndex("type"));

                if (protocol != null || type != (int)SmsMessageType.Sent)
                    return;

                string from = _telephonyManager.Line1Number;
                string to = cursor.GetString(cursor.GetColumnIndex("address"));
                long unixTimeMS = cursor.GetLong(cursor.GetColumnIndex("date"));
                DateTimeOffset dotNetDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(unixTimeMS);
                string message = cursor.GetString(cursor.GetColumnIndex("body"));
                cursor.Close();

                _outgoingSMS(new SmsDatum(_probe, dotNetDateTime, from, to, message));
            }
        }
    }
}