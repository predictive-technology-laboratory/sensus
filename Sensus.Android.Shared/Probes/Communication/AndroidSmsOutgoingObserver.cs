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
using Sensus.Exceptions;
using Sensus.Probes.Communication;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsOutgoingObserver : ContentObserver
    {
        private readonly Action<SmsDatum> _outgoingSmsCallback;
        private string _mostRecentlyObservedSmsURI;

        public AndroidSmsOutgoingObserver(Action<SmsDatum> outgoingSmsCallback)
            : base(null)
        {
            _outgoingSmsCallback = outgoingSmsCallback;
            _mostRecentlyObservedSmsURI = null;
        }

        public override void OnChange(bool selfChange)
        {
            OnChange(selfChange, global::Android.Net.Uri.Parse("content://sms"));
        }

        public override void OnChange(bool selfChange, global::Android.Net.Uri uri)
        {
            // might be getting null URIs
            if (uri == null)
            {
                return;
            }

            // for some reason, we get multiple calls to OnChange for the same outgoing text. ignore repeats.
            if (_mostRecentlyObservedSmsURI != null && uri.ToString() == _mostRecentlyObservedSmsURI)
            {
                return;
            }

            // TODO:  Fix issue #75 -- need to handle MMS. they are structured differently than SMS, and the code below does not work.
            if (uri.ToString().StartsWith("content://sms/raw"))
            {
                return;
            }

            ICursor cursor = global::Android.App.Application.Context.ContentResolver.Query(uri, null, null, null, null);

            if (cursor.MoveToNext())
            {
                // we've been seeing some issues with missing fields. catch any exceptions that occur here and report them.
                try
                {
                    string protocol = cursor.GetString(cursor.GetColumnIndexOrThrow("protocol"));
                    int type = cursor.GetInt(cursor.GetColumnIndexOrThrow("type"));

                    int sentMessageType;

                    // see the Backwards Compatibility article for more information
#if __ANDROID_19__
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                    {
                        sentMessageType = (int)SmsMessageType.Sent;  // API level 19
                    }
                    else
#endif
                    {
                        sentMessageType = 2;
                    }

                    if (protocol != null || type != sentMessageType)
                    {
                        return;
                    }

                    string toNumber = cursor.GetString(cursor.GetColumnIndexOrThrow("address"));
                    long unixTimeMS = cursor.GetLong(cursor.GetColumnIndexOrThrow("date"));
                    DateTimeOffset dotNetDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(unixTimeMS);
                    string message = cursor.GetString(cursor.GetColumnIndexOrThrow("body"));

                    _outgoingSmsCallback?.Invoke(new SmsDatum(dotNetDateTime, null, toNumber, message, true));

                    _mostRecentlyObservedSmsURI = uri.ToString();
                }
                catch (Exception ex)
                {
                    // something is wrong with our implementation
                    SensusException.Report(ex);
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