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
using System.IO;
using System.Text;
using Android.App;
using Android.Database;
using Android.OS;
using Android.Provider;
using Sensus.Exceptions;
using Sensus.Probes.Communication;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsOutgoingObserver : ContentObserver
    {
        private Action<SmsDatum> _outgoingSmsCallback;
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
            // for some reason, we get multiple calls to OnChange for the same outgoing text. ignore repeats.
            if (_mostRecentlyObservedSmsURI != null && uri.ToString() == _mostRecentlyObservedSmsURI)
            {
                return;
            }

            string body = null, toNumber = null;
            int type = -1;
            long unixTimeMS = -1;
            ICursor queryResults = null;
            try
            {
                // process MMS:  https://stackoverflow.com/questions/3012287/how-to-read-mms-data-in-android
                if (uri.ToString().StartsWith("content://sms/raw") || uri.ToString().StartsWith("content://mms-sms"))
                {
                    queryResults = Application.Context.ContentResolver.Query(global::Android.Net.Uri.Parse("content://mms-sms/conversations/"), null, null, null, "_id");

                    if (queryResults.MoveToLast())
                    {
                        unixTimeMS = queryResults.GetLong(queryResults.GetColumnIndexOrThrow("date")) * 1000;

                        int messageId = queryResults.GetInt(queryResults.GetColumnIndexOrThrow("_id"));
                        ICursor innerQueryResults = Application.Context.ContentResolver.Query(global::Android.Net.Uri.Parse("content://mms/part"), null, "mid=" + messageId, null, null);

                        try
                        {
                            if (innerQueryResults.MoveToFirst())
                            {
                                if (innerQueryResults.GetString(innerQueryResults.GetColumnIndexOrThrow("ct")) == "text/plain")
                                {
                                    string data = innerQueryResults.GetString(innerQueryResults.GetColumnIndexOrThrow("_data"));

                                    if (data == null)
                                    {
                                        body = innerQueryResults.GetString(innerQueryResults.GetColumnIndexOrThrow("text"));
                                    }
                                    else
                                    {
                                        int partId = innerQueryResults.GetInt(innerQueryResults.GetColumnIndexOrThrow("_id"));
                                        body = GetMmsText(partId);
                                    }

                                    toNumber = GetAddressNumber(messageId, 151); // 137 is the from and 151 is the to
                                }
                            }
                        }
                        finally
                        {
                            // always close cursor
                            try
                            {
                                innerQueryResults.Close();
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                // proces SMS
                else
                {
                    queryResults = Application.Context.ContentResolver.Query(uri, null, null, null, null);

                    if (queryResults.MoveToNext())
                    {
                        string protocol = queryResults.GetString(queryResults.GetColumnIndexOrThrow("protocol"));
                        type = queryResults.GetInt(queryResults.GetColumnIndexOrThrow("type"));

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

                        toNumber = queryResults.GetString(queryResults.GetColumnIndexOrThrow("address"));
                        unixTimeMS = queryResults.GetLong(queryResults.GetColumnIndexOrThrow("date"));
                        body = queryResults.GetString(queryResults.GetColumnIndexOrThrow("body"));
                    }
                }

                if (!string.IsNullOrWhiteSpace(body))
                {
                    _outgoingSmsCallback(new SmsDatum(DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMS), null, toNumber, body, true));
                    _mostRecentlyObservedSmsURI = uri.ToString();
                }
            }
            catch (System.Exception ex)
            {
                // something is wrong with our implementation
                SensusException.Report(ex);
            }
            finally
            {
                // always close cursor
                try
                {
                    queryResults.Close();
                }
                catch
                {
                }
            }
        }

        private string GetMmsText(int id)
        {
            string text = null;

            Stream inputStream = null;
            try
            {
                inputStream = Application.Context.ContentResolver.OpenInputStream(global::Android.Net.Uri.Parse("content://mms/part/" + id.ToString()));

                if (inputStream != null)
                {
                    text = new StreamReader(inputStream, Encoding.UTF8).ReadToEnd();
                }
            }
            finally
            {
                try
                {
                    inputStream?.Close();
                }
                catch { }
            }

            return text;
        }

        private String GetAddressNumber(int id, int type)
        {
            string number = null;

            ICursor queryResults = Application.Context.ContentResolver.Query(global::Android.Net.Uri.Parse($"content://mms/{id}/addr"), null, "msg_id=" + id, null, null);
            try
            {
                while (number == null && queryResults.MoveToNext())
                {
                    if (queryResults.GetInt(queryResults.GetColumnIndexOrThrow("type")) == type)
                    {
                        number = queryResults.GetString(queryResults.GetColumnIndexOrThrow("address"));

                        if (number != null)
                        {
                            try
                            {
                                // ensure we have a string of digits
                                long.Parse(number.Replace("-", ""));
                            }
                            catch (Exception)
                            {
                                number = null;
                            }
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    queryResults?.Close();
                }
                catch { }
            }

            return number;
        }
    }
}