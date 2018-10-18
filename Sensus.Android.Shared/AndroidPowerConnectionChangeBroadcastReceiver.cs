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