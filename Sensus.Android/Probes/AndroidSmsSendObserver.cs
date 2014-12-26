using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Telephony;
using SensusService.Probes.Communication;
using System;

namespace Sensus.Android.Probes
{
    public class AndroidSmsSendObserver : ContentObserver
    {
        public event EventHandler<SmsDatum> MessageSent;

        private Context _context;
        private TelephonyManager _telephonyManager;

        public AndroidSmsSendObserver(Context context)
            : base(null)
        {
            _context = context;
            _telephonyManager = _context.GetSystemService(Context.TelephonyService) as TelephonyManager;
        }

        public override void OnChange(bool selfChange)
        {
            OnChange(selfChange, global::Android.Net.Uri.Parse("content://sms"));
        }

        public override void OnChange(bool selfChange, global::Android.Net.Uri uri)
        {
            lock (this)
            {
                ICursor cursor = _context.ContentResolver.Query(uri, null, null, null, null);
                if (cursor.MoveToNext())
                {
                    string protocol = cursor.GetString(cursor.GetColumnIndex("protocol"));
                    int type = cursor.GetInt(cursor.GetColumnIndex("type"));

                    if (MessageSent == null || protocol != null || type != (int)SmsMessageType.Sent)
                        return;

                    string from = _telephonyManager.Line1Number;
                    string to = cursor.GetString(cursor.GetColumnIndex("address"));
                    long unixTimeMS = cursor.GetLong(cursor.GetColumnIndex("date"));
                    DateTimeOffset dotNetDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(unixTimeMS);
                    string message = cursor.GetString(cursor.GetColumnIndex("body"));

                    MessageSent(this, new SmsDatum(null, dotNetDateTime, from, to, message));
                }
            }
        }

        public void Stop()
        {
            lock (this)
                MessageSent = null;
        }
    }
}