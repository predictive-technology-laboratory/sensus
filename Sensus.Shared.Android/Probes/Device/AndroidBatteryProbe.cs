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

using Android.OS;
using Android.App;
using Android.Content;
using System;
using System.Threading;
using System.Collections.Generic;
using Sensus.Shared;
using Sensus.Shared.Probes.Device;


namespace Sensus.Shared.Android.Probes.Device
{
    public class AndroidBatteryProbe : BatteryProbe
    {
        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            Intent lastIntent = Application.Context.RegisterReceiver(null, new IntentFilter(Intent.ActionBatteryChanged));
            if (lastIntent == null)
                throw new Exception("Failed to poll battery status.");
            else
                return new BatteryDatum[] { new BatteryDatum(DateTimeOffset.UtcNow, lastIntent.GetIntExtra(BatteryManager.ExtraLevel, -1)) };
        }
    }
}
