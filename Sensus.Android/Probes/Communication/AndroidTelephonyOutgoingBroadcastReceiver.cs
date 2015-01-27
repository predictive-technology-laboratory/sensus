#region copyright
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
#endregion

using Android.App;
using Android.Content;
using System;

namespace Sensus.Android.Probes.Communication
{
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionNewOutgoingCall }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidTelephonyOutgoingBroadcastReceiver : BroadcastReceiver
    {
        public static event EventHandler<string> OutgoingCall;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (OutgoingCall != null && intent != null && intent.Action == Intent.ActionNewOutgoingCall)
                OutgoingCall(this, intent.GetStringExtra(Intent.ExtraPhoneNumber));
        }
    }
}