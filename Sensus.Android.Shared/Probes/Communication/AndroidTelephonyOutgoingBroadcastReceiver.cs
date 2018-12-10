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
using Android.Content;
using Sensus.Exceptions;
using System;

namespace Sensus.Android.Probes.Communication
{
    /// <summary>
    /// Listens for new outgoing calls. See <see cref="AndroidTelephonyIdleIncomingListener"/> for why we need both classes.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionNewOutgoingCall }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidTelephonyOutgoingBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<string> OUTGOING_CALL;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

                if (intent.Action == Intent.ActionNewOutgoingCall)
                {
                    OUTGOING_CALL?.Invoke(this, intent.GetStringExtra(Intent.ExtraPhoneNumber));
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception in telephony broadcast receiver:  " + ex.Message, ex);
            }
        }
    }
}
