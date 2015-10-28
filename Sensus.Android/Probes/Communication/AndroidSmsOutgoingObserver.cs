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
using Xamarin;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsOutgoingObserver : ContentObserver
    {
        private global::Android.Content.Context _context;
        private Action<SmsDatum> _outgoingSMS;
        private string _mostRecentlyObservedSmsURI;

        public AndroidSmsOutgoingObserver(global::Android.Content.Context context, Action<SmsDatum> outgoingSmsCallback)
            : base(null)
        {
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

            // TODO:  Fix issue #75 -- need to handle MMS. they are structured differently than SMS, and the code below does not work.
            if (uri.ToString() == "content://sms/raw")
                return;

            ICursor cursor = _context.ContentResolver.Query(uri, null, null, null, null);

            if (cursor.MoveToNext())
            {
                // we've been seeing some issues with missing fields:  https://insights.xamarin.com/app/Sensus-Production/issues/23
                // catch any exceptions that occur here and report them.
                try
                {
                    string protocol = cursor.GetString(cursor.GetColumnIndexOrThrow("protocol"));
                    int type = cursor.GetInt(cursor.GetColumnIndexOrThrow("type"));

                    int sentMessageType;

                    // https://github.com/predictive-technology-laboratory/sensus/wiki/Backwards-Compatibility
                    #if __ANDROID_19__
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                        sentMessageType = (int)SmsMessageType.Sent;  // API level 19
                    else
                    #endif
                        sentMessageType = 2;

                    if (protocol != null || type != sentMessageType)
                        return;

                    string toNumber = cursor.GetString(cursor.GetColumnIndexOrThrow("address"));
                    long unixTimeMS = cursor.GetLong(cursor.GetColumnIndexOrThrow("date"));
                    DateTimeOffset dotNetDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(unixTimeMS);
                    string message = cursor.GetString(cursor.GetColumnIndexOrThrow("body"));

                    _outgoingSMS(new SmsDatum(dotNetDateTime, null, toNumber, message));

                    _mostRecentlyObservedSmsURI = uri.ToString();
                }
                catch(Exception ex)
                {
                    // if anything goes wrong, report exception to Insights
                    try
                    {
                        Insights.Report(ex, Insights.Severity.Error);
                    }
                    catch
                    {
                    }
                }
                finally
                {
                    // always close cursor
                    try
                    {
                        cursor.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}