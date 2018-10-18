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
