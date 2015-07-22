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
using System.Linq;
using SensusService;
using System.Threading;
using SensusService.Probes;
using CoreTelephony;
using SensusService.Probes.Communication;

namespace Sensus.iOS
{
    public class iOSTelephonyProbe : PollingProbe
    {

        static List <CTCall> calls = new List<CTCall>();    // list of calls made or received
        static List <CTCall> temp = new List<CTCall>();     // copy of calls list, to perform duplicate filtering

        TelephonyState state = TelephonyState.Idle;         // Default set to Idle (temporary, TODO maybe pull call state info out and pass to TelephonyDatum object)
        int datumListIndex = 0;

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected sealed override string DefaultDisplayName
        {
            get {
                return "Telephony";
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get { return 600000; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(TelephonyDatum); }
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            var data1 = CallLogger.Instance;
            calls = data1.CallLog();                // calls receives list of call events from Singleton (CallLogger)
            temp = data1.CallLog();                 // duplicate to temp
            datumListIndex = calls.Count;
            Datum[] datumList = new Datum[datumListIndex];
            int i = 0;
            int j = 0;
            int count = 0;

            // filter out duplicates (call events), determine # of calls
            for (i = 0; i < temp.Count; ++i)
            {
                for (j = i+1; j < temp.Count; ++j)
                {
                    if (temp.ElementAt(i).CallID == temp.ElementAt(j).CallID)
                    {
                        temp.RemoveAt(j);
                        --j;
                    }
                }
            }

            // creates TelephonyDatum objects
            while (count < temp.Count)
            {
                datumList[count] = new TelephonyDatum(DateTimeOffset.UtcNow, state, null);
                ++count;
            }
            calls.Clear();
            temp.Clear();
            return datumList;
        }
    }
}


