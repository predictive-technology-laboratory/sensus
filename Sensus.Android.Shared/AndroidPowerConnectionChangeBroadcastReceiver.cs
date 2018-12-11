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

using Android.Content;
using Sensus.Exceptions;
using System;

namespace Sensus.Android
{
    public class AndroidPowerConnectionChangeBroadcastReceiver : BroadcastReceiver
    {
        /// <summary>
        /// Occurs when the phone is either plugged into (true) or removed from (false) an external power source.
        /// </summary>
        public static event EventHandler<bool> POWER_CONNECTION_CHANGED;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

                if (intent.Action == Intent.ActionPowerConnected || intent.Action == Intent.ActionPowerDisconnected)
                {
                    POWER_CONNECTION_CHANGED?.Invoke(this, intent.Action == Intent.ActionPowerConnected);
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception in power connection change broadcast receiver:  " + ex.Message, ex);
            }
        }
    }
}
