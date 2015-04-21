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
using SensusService.Probes.Network;
using Foundation;
using SystemConfiguration;
using System.Collections.Generic;
using SensusService;
using System.Threading;

namespace Sensus.iOS.Network.Probes
{
    public class iOSPollingWlanProbe : PollingWlanProbe
    {
        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            NSDictionary networkInfo;

            if (CaptiveNetwork.TryCopyCurrentNetworkInfo("en0", out networkInfo) == StatusCode.NoKey || networkInfo == null)
                return new Datum[] { };

            return new Datum[] { new WlanDatum(DateTimeOffset.UtcNow, networkInfo[CaptiveNetwork.NetworkInfoKeyBSSID].ToString()) };
        }
    }
}