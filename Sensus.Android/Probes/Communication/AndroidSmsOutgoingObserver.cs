// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Android.Database;
using Android.OS;
using Android.Provider;
using SensusService.Probes;
using SensusService.Probes.Communication;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsOutgoingObserver : ContentObserver
    {
        private Probe _probe;
        private global::Android.Content.Context _context;
        private Action<SmsDatum> _outgoingSMS;
        private string _mostRecentlyObservedSmsURI;

        public AndroidSmsOutgoingObserver(Probe probe, global::Android.Content.Context context, Action<SmsDatum> outgoingSmsCallback)
            : base(null)
        {
            _probe = probe;
            _context = context;
            _outgoingSMS = outgoingSmsCallback;
            _mostRecentlyObservedSmsURI = null;
        }

        public override void OnChange(bool selfChange)
        {
            OnChange(selfChange, global::Android.Net.Uri.Parse("content://sms"));
        }

        public override void OnChange(bool selfChange, global::Android.Net.Uri uri)
        {
            // for some reason, we get multiple calls to OnChange for the same outgoing text. ignore repeats.
            if (_mostRecentlyObservedSmsURI != null && uri.ToString() == _mostRecentlyObservedSmsURI)
                return;

            ICursor cursor = _context.ContentResolver.Query(uri, null, null, null, null);
            if (cursor.MoveToNext())
            {
                string protocol = cursor.GetString(cursor.GetColumnIndex("protocol"));
                int type = cursor.GetInt(cursor.GetColumnIndex("type"));

                int sentMessageType;

                #if __ANDROID_19__
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                    sentMessageType = (int)SmsMessageType.Sent;  // API level 19
                else
                #endif
                    sentMessageType = 2;

                if (protocol != null || type != sentMessageType)
                    return;

                string to = cursor.GetString(cursor.GetColumnIndex("address"));
                long unixTimeMS = cursor.GetLong(cursor.GetColumnIndex("date"));
                DateTimeOffset dotNetDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(unixTimeMS);
                string message = cursor.GetString(cursor.GetColumnIndex("body"));
                cursor.Close();

                _outgoingSMS(new SmsDatum(_probe, dotNetDateTime, null, to, message));

                _mostRecentlyObservedSmsURI = uri.ToString();
            }
        }
    }
}
