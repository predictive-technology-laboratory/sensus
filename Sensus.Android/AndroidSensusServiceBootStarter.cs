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

namespace Sensus.Android
{
    /// <summary>
    /// Starts Sensus service on boot completion.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionBootCompleted }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidSensusServiceBootStarter : BroadcastReceiver
    {
        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (intent.Action == Intent.ActionBootCompleted)
                context.StartService(new Intent(context, typeof(AndroidSensusService)));
        }
    }
}