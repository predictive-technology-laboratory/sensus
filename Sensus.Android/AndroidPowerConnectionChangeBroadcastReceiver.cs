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

namespace Sensus.Android
{
    /// <summary>
    /// https://developer.android.com/reference/android/content/Intent#ACTION_POWER_CONNECTED
    /// that wish to register specifically to this notification. Unlike ACTION_BATTERY_CHANGED, 
    /// applications will be woken for this and so do not have to stay active to receive this notification. 
    /// This action can be used to implement actions that wait until power is available to trigger.
    /// added in API level 4
    /// 
    /// We use the same receiver for both the connected and disconnected intents.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionPowerConnected, Intent.ActionPowerDisconnected }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidPowerConnectionChangeBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<bool> POWER_CONNECTION_CHANGED;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            try
            {
                if (POWER_CONNECTION_CHANGED != null &&
                    intent != null &&
                    (intent.Action == Intent.ActionPowerConnected || intent.Action == Intent.ActionPowerDisconnected))
                {
                    POWER_CONNECTION_CHANGED(this, intent.Action == Intent.ActionPowerConnected);
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Failed to process power connection change intent.", ex);
            }
        }
    }
}