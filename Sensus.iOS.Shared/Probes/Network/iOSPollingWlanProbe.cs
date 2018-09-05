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
using Sensus.Probes.Network;
using Foundation;
using SystemConfiguration;
using System.Collections.Generic;
using Sensus;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Sensus.iOS.Network.Probes
{
    public class iOSPollingWlanProbe : PollingWlanProbe
    {
        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            NSDictionary networkInfo;

            string bssid = null;
            if (CaptiveNetwork.TryCopyCurrentNetworkInfo("en0", out networkInfo) != StatusCode.NoKey && networkInfo != null)
            {
                bssid = networkInfo[CaptiveNetwork.NetworkInfoKeyBSSID].ToString();
            }

            return Task.FromResult(new Datum[] { new WlanDatum(DateTimeOffset.UtcNow, bssid) }.ToList());
        }
    }
}