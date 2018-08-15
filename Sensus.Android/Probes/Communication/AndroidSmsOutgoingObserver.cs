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
            // for some reason, we get multiple calls to OnChange for the same outgoing text. ignore repeats.
            if (_mostRecentlyObservedSmsURI != null && uri.ToString() == _mostRecentlyObservedSmsURI)
            {
                return;
            }

            // TODO:  Fix issue #75 -- need to handle MMS. they are structured differently than SMS, and the code below does not work.

            var contentResolver = _context.ContentResolver;

            //if (uri.ToString().StartsWith("content://sms/raw"))
            //{

            var c = true;
            var baseUri = global::Android.Net.Uri.Parse("content://mms-sms/conversations/");
            var query = contentResolver.Query(baseUri, null, null, null, "_id");
            query.MoveToLast();
            while (c)
            {
                var t = query.GetString(query.GetColumnIndex("ct_t"));
                if (t != null)
                {
                    var isMMS = t == "application/vnd.wap.multipart.related" || t == "application/vnd.wap.multipart.mixed";
                    var messageId = query.GetString(query.GetColumnIndex("_id"));
                    if (isMMS)
                    {
                        // it's MMS
                        var p = "content://mms/part";

                        var innerCur = _context.ContentResolver.Query(global::Android.Net.Uri.Parse(p), null, "mid=" + messageId, null, null);
                        if (innerCur != null && innerCur.MoveToFirst())
                        {
                            var go = true;
                            while (go)
                            {
                                try
                                {
                                    var type = innerCur.GetString(innerCur.GetColumnIndex("ct"));
                                    if (type == "text/plain")
                                    {
                                        var names = innerCur.GetColumnNames();
                                        System.Collections.Generic.List<string> vals = new System.Collections.Generic.List<string>();
                                        names.ToList().ForEach(e => vals.Add($"{e}:{innerCur.GetString(innerCur.GetColumnIndex(e))}"));
                                        var data = innerCur.GetString(innerCur.GetColumnIndex("_data"));
                                        var partId = innerCur.GetString(innerCur.GetColumnIndex("_id"));
                                        var body = string.Empty;
                                        if (data != null)
                                        {
                                            // implementation of this method below
                                            body = GetMmsText(partId);
                                        }
                                        else
                                        {
                                            body = innerCur.GetString(innerCur.GetColumnIndex("text"));
                                        }
                                        var num = getAddressNumber(int.Parse(partId));
                                    }
                                    go = innerCur.MoveToNext(); //TODO: not sure why we cannot do a do while loop but it isn't working.
                                }
                                catch (Exception ex)
                                {
                                    var e = ex;
                                    go = innerCur.MoveToNext();
                                }
                            }
                        }
                    }
                    else
                    {
                        var innerCur = contentResolver.Query(uri, null, null, null, null);
                        if (innerCur != null && innerCur.MoveToNext())
                        {
                            var phone = innerCur.GetString(innerCur.GetColumnIndex("address"));
                            var type = innerCur.GetInt(innerCur.GetColumnIndex("type"));// 2 = sent, etc.
                            var date = innerCur.GetString(innerCur.GetColumnIndex("date"));
                            var body = innerCur.GetString(innerCur.GetColumnIndex("body"));
                        }
                    }
                }
                c = query.MoveToPrevious();
            }
        }





        //return;


        //}

        //            ICursor cursor = _context.ContentResolver.Query(uri, null, null, null, null);

        //            if (cursor.MoveToNext())
        //            {
        //                // we've been seeing some issues with missing fields. catch any exceptions that occur here and report them.
        //                try
        //                {
        //                    string protocol = cursor.GetString(cursor.GetColumnIndexOrThrow("protocol"));
        //                    int type = cursor.GetInt(cursor.GetColumnIndexOrThrow("type"));

        //                    int sentMessageType;

        //                    // see the Backwards Compatibility article for more information
        //#if __ANDROID_19__
        //                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
        //                    {
        //                        sentMessageType = (int)SmsMessageType.Sent;  // API level 19
        //                    }
        //                    else
        //#endif
        //                    {
        //                        sentMessageType = 2;
        //                    }

        //                    if (protocol != null || type != sentMessageType)
        //                    {
        //                        return;
        //                    }

        //                    string toNumber = cursor.GetString(cursor.GetColumnIndexOrThrow("address"));
        //                    long unixTimeMS = cursor.GetLong(cursor.GetColumnIndexOrThrow("date"));
        //                    DateTimeOffset dotNetDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(unixTimeMS);
        //                    string message = cursor.GetString(cursor.GetColumnIndexOrThrow("body"));

        //                    _outgoingSMS(new SmsDatum(dotNetDateTime, null, toNumber, message, true));

        //                    _mostRecentlyObservedSmsURI = uri.ToString();
        //                }
        //                catch (System.Exception ex)
        //                {
        //                    // something is wrong with our implementation
        //                    SensusException.Report(ex);
        //                }
        //                finally
        //                {
        //                    // always close cursor
        //                    try
        //                    {
        //                        cursor.Close();
        //                    }
        //                    catch
        //                    {
        //                    }
        //                }
        //}
        //}

        private string GetMmsText(string id)
        {
            var partURI = global::Android.Net.Uri.Parse("content://mms/part/" + id);
            StringBuilder sb = new StringBuilder();
            try
            {
                using (var inputStream = _context.ContentResolver.OpenInputStream(partURI))
                {
                    if (inputStream != null)
                    {
                        var temp = new StreamReader(inputStream).ReadToEnd();
                        sb.Append(temp);
                    }
                }
            }
            catch (IOException e) { }
            return sb.ToString();
        }
        private String getAddressNumber(int id)
        {
            var selectionAdd = "msg_id=" + id;
            var uriStr = $"content://mms/{id}/addr";
            var uriAddress = global::Android.Net.Uri.Parse(uriStr);
            var cAdd = _context.ContentResolver.Query(uriAddress, null,
                selectionAdd, null, null);
            String name = null;
            if (cAdd.MoveToFirst())
            {
                do
                {
                    String number = cAdd.GetString(cAdd.GetColumnIndex("address"));
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
                } while (cAdd.MoveToNext());
            }
            if (cAdd != null)
            {
                cAdd.Close();
            }
            return name;
        }
    }
}