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

using System;
using System.Collections.Generic;
using System.Threading;
using SensusService;
using SensusService.Probes.Device;
using UIKit;

namespace Sensus.iOS.Probes.Device
{
    public class iOSBatteryProbe : BatteryProbe
    {
        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            Console.Out.WriteLine("Hello1");
            return new Datum[] { new BatteryDatum(DateTimeOffset.UtcNow, (int)(UIDevice.CurrentDevice.BatteryLevel * 100)) };  // report value [0,100] to be same as android
        }
    }
}