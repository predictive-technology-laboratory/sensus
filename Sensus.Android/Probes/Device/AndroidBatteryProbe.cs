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
using Android.OS;
using SensusService.Probes.Device;
using System;
using System.Collections.Generic;

namespace Sensus.Android.Probes.Device
{
    public class AndroidBatteryProbe : BatteryProbe
    {
        protected override IEnumerable<SensusService.Datum> Poll()
        {
            Intent lastIntent = Application.Context.RegisterReceiver(null, new IntentFilter(Intent.ActionBatteryChanged));
            if (lastIntent == null)
                return new BatteryDatum[] { };
            else
                return new BatteryDatum[] { new BatteryDatum(this, DateTimeOffset.UtcNow, lastIntent.GetIntExtra(BatteryManager.ExtraLevel, -1)) };
        }
    }
}