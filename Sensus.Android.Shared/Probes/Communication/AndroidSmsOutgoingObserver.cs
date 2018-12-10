//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Android.App;
using Android.Database;
using Android.OS;
using Android.Provider;
using Sensus.Exceptions;
using Sensus.Probes.Communication;
using System;
using System.IO;
using System.Text;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidSmsOutgoingObserver : ContentObserver
    {
        private readonly Action<SmsDatum> _outgoingSmsCallback;
        private string _mostRecentlyObservedSmsURI;
        private DateTimeOffset? _mostRecentMmsTimestamp;

        public AndroidSmsOutgoingObserver(Action<SmsDatum> outgoingSmsCallback)
            : base(null)
        {
            _outgoingSmsCallback = outgoingSmsCallback;
        }

        public void Initialize()
        {
            _mostRecentlyObservedSmsURI = null;
            _mostRecentMmsTimestamp = GetMostRecentMMS()?.Timestamp; // get the most recent mms so we don't duplicate it
        }

        public override void OnChange(bool selfChange)
        {
            try
            {
                OnChange(selfChange, global::Android.Net.Uri.Parse("content://sms"));
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception in OnChange:  " + ex.Message, ex);
            }
        }

        public override void OnChange(bool selfChange, global::Android.Net.Uri uri)
        {
            try
            {
                // for some reason, we get multiple calls to OnChange for the same outgoing text. ignore repeats.
                if (_mostRecentlyObservedSmsURI != null && uri.ToString() == _mostRecentlyObservedSmsURI)
                {
                    return;
                }

                SmsDatum datum = null;

                // process MMS:  https://stackoverflow.com/questions/3012287/how-to-read-mms-data-in-android
                bool isMMS = uri.ToString().StartsWith("content://sms/raw") || uri.ToString().StartsWith("content://mms-sms");
                if (isMMS)
                {
                    datum = GetMostRecentMMS();
                }
                else
                {
                    datum = GetSms(uri);
                }

                if (!string.IsNullOrWhiteSpace(datum?.Message))
                {
                    _outgoingSmsCallback?.Invoke(datum);

                    if (isMMS)
                    {
                        _mostRecentMmsTimestamp = datum.Timestamp;
                    }
                    else
                    {
                        _mostRecentlyObservedSmsURI = uri.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // something is probably wrong with our implementation. each manufacturer does things a bit different.
                SensusServiceHelper.Get().Logger.Log("Exception in " + nameof(OnChange) + ":  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        private SmsDatum GetMostRecentMMS()
        {
            SmsDatum mmsDatum = null;

            ICursor queryResults = null;
            try
            {
                // get the most recent conversation
                queryResults = Application.Context.ContentResolver.Query(global::Android.Net.Uri.Parse("content://mms-sms/conversations/"), null, null, null, "_id");

                if (queryResults.MoveToLast())
                {
                    long unixTimeMS = queryResults.GetLong(queryResults.GetColumnIndexOrThrow("date")) * 1000;
                    int messageId = queryResults.GetInt(queryResults.GetColumnIndexOrThrow("_id"));

                    ICursor innerQueryResults = Application.Context.ContentResolver.Query(global::Android.Net.Uri.Parse("content://mms/part"), null, "mid=" + messageId, null, null);

                    try
                    {
                        if (innerQueryResults.MoveToFirst())
                        {
                            while (true)
                            {
                                if (innerQueryResults.GetString(innerQueryResults.GetColumnIndexOrThrow("ct")) == "text/plain")
                                {
                                    string toNumber = GetAddressNumber(messageId, 151); // 137 is the from and 151 is the to
                                    DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMS);

                                    // get the message body
                                    string data = innerQueryResults.GetString(innerQueryResults.GetColumnIndexOrThrow("_data"));
                                    string body;
                                    if (data == null)
                                    {
                                        body = innerQueryResults.GetString(innerQueryResults.GetColumnIndexOrThrow("text"));
                                    }
                                    else
                                    {
                                        int partId = innerQueryResults.GetInt(innerQueryResults.GetColumnIndexOrThrow("_id"));
                                        body = GetMmsText(partId);
                                    }

                                    // only keep if we have a message body
                                    if (!string.IsNullOrWhiteSpace(body) && _mostRecentMmsTimestamp != timestamp)
                                    {
                                        mmsDatum = new SmsDatum(timestamp, null, toNumber, body, true);
                                    }

                                    break;
                                }
                                else
                                {
                                    if (!innerQueryResults.MoveToNext())
                                    {
                                        break;
                                    }
                                }
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
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting MMS:  " + ex.Message, LoggingLevel.Normal, GetType());

                // throw back to caller
                throw ex;
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

            return mmsDatum;
        }

        private SmsDatum GetSms(global::Android.Net.Uri uri)
        {
            SmsDatum smsDatum = null;
            ICursor queryResults = null;
            try
            {
                queryResults = Application.Context.ContentResolver.Query(uri, null, null, null, null);

                if (queryResults != null && queryResults.MoveToNext())
                {
                    string protocol = queryResults.GetString(queryResults.GetColumnIndex("protocol"));
                    var type = queryResults.GetInt(queryResults.GetColumnIndex("type"));

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

                    if (type != sentMessageType) //note:protocol is never coming in null for me
                    {
                        return null;
                    }

                    string toNumber = queryResults.GetString(queryResults.GetColumnIndexOrThrow("address"));
                    long unixTimeMS = queryResults.GetLong(queryResults.GetColumnIndexOrThrow("date"));
                    string body = queryResults.GetString(queryResults.GetColumnIndexOrThrow("body"));
                    DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMS);

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        smsDatum = new SmsDatum(timestamp, null, toNumber, body, true);
                    }
                }
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
            return smsDatum;
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

        private string GetAddressNumber(int id, int type)
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
