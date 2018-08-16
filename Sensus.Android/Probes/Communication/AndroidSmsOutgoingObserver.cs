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
using System.Linq;
using System.Text;
using Android.Database;
using Android.OS;
using Android.Provider;
using Sensus.Exceptions;
using Sensus.Probes.Communication;

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
            //TODO:  It seems like this always fires on receipt, but it doesn't consistantly fire on MMS send for my LG G6 and it cannot be tested on the emulator.
            // for some reason, we get multiple calls to OnChange for the same outgoing text. ignore repeats.
            if (_mostRecentlyObservedSmsURI != null && uri.ToString() == _mostRecentlyObservedSmsURI)
            {
                return;
            }

            global::Android.Content.ContentResolver contentResolver = _context.ContentResolver;

            string body = null, toNumber = null;
            int type = -1;
            long unixTimeMS = -1;
            ICursor query = null;
            try
            {
                // https://stackoverflow.com/questions/3012287/how-to-read-mms-data-in-android
                if (uri.ToString().StartsWith("content://sms/raw")) //is it an mms?
                {
                    query = contentResolver.Query(global::Android.Net.Uri.Parse("content://mms-sms/conversations/"), null, null, null, "_id");
                    if (query.MoveToLast())
                    {
                        //TODO:  Do we want to filter out some message types/protocols like we do for the SMS?

                        string messageType = query.GetString(query.GetColumnIndexOrThrow("ct_t"));

                        if (messageType == "application/vnd.wap.multipart.related" || messageType == "application/vnd.wap.multipart.mixed")
                        {
                            unixTimeMS = query.GetLong(query.GetColumnIndexOrThrow("date")) * 1000;
                            int messageId = query.GetInt(query.GetColumnIndexOrThrow("_id"));
                            ICursor innerQuery = _context.ContentResolver.Query(global::Android.Net.Uri.Parse("content://mms/part"), null, "mid=" + messageId.ToString(), null, null);
                            try
                            {
                                if (innerQuery != null)
                                {
                                    innerQuery.MoveToFirst();
                                    do
                                    {
                                        if (innerQuery.GetString(innerQuery.GetColumnIndexOrThrow("ct")) == "text/plain")
                                        {
                                            string data = innerQuery.GetString(innerQuery.GetColumnIndexOrThrow("_data"));
                                            int partId = innerQuery.GetInt(innerQuery.GetColumnIndexOrThrow("_id"));

                                            if (data != null)
                                            {
                                                body = GetMmsText(partId);
                                            }
                                            else
                                            {
                                                body = innerQuery.GetString(innerQuery.GetColumnIndexOrThrow("text"));
                                            }
                                            toNumber = getAddressNumber(messageId, 151); //137 is the from and 151 is the to
                                        }

                                    }
                                    while (innerQuery.MoveToNext());
                                }
                            }
                            finally
                            {
                                // always close cursor
                                try
                                {
                                    innerQuery.Close();
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                else //it is an SMS
                {
                    query = _context.ContentResolver.Query(uri, null, null, null, null);
                    if (query.MoveToNext())
                    {
                        string protocol = query.GetString(query.GetColumnIndexOrThrow("protocol"));
                        type = query.GetInt(query.GetColumnIndexOrThrow("type"));

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

                        if (protocol != null || type != sentMessageType) //TODO:  I am often getting a protocol of "0" for my sent sms messages.  
                        {
                            return;
                        }

                        toNumber = query.GetString(query.GetColumnIndexOrThrow("address"));
                        unixTimeMS = query.GetLong(query.GetColumnIndexOrThrow("date"));
                        body = query.GetString(query.GetColumnIndexOrThrow("body"));
                    }
                }
                if (string.IsNullOrWhiteSpace(body) == false)
                {
                    DateTimeOffset dotNetDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(unixTimeMS);
                    _outgoingSMS(new SmsDatum(dotNetDateTime, null, toNumber, body, true));

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
                    query.Close();
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
                inputStream = _context.ContentResolver.OpenInputStream(global::Android.Net.Uri.Parse("content://mms/part/" + id.ToString()));

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

        private String getAddressNumber(int id, int type)
        {
            string name = null;
            ICursor cAdd = _context.ContentResolver.Query(global::Android.Net.Uri.Parse($"content://mms/{id}/addr"), null, "msg_id=" + id, null, null);
            try
            {
                if (cAdd.MoveToFirst())
                {
                    do
                    {
                        if (cAdd.GetInt(cAdd.GetColumnIndexOrThrow("type")) == type) //137 is the from and 151 is the too
                        {
                            string number = cAdd.GetString(cAdd.GetColumnIndexOrThrow("address"));
                            if (number != null)
                            {
                                try
                                {
                                    long.Parse(number.Replace("-", ""));
                                    name = number;
                                }
                                catch (Exception nfe)
                                {
                                    if (name == null)
                                    {
                                        name = number;
                                    }
                                }
                            }
                        }
                    } while (cAdd.MoveToNext());
                }
            }
            finally
            {
                try
                {
                    cAdd?.Close();
                }
                catch { }
            }

            return name;
        }
    }
}