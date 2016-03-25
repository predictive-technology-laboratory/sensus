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

namespace Sensus.iOS.Probes.Communication
{
    public class iOSTelephonyProbe : PollingTelephonyProbe
    {
        private List <CTCall> _calls;
        private CTCallCenter _callCenter1;
        private CTCallCenter _callCenter2;

        protected override void Initialize()
        {
            base.Initialize();

            _calls = new List<CTCall>();

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    // for some reason, using a single CTCallCenter misses calls when the app is resumed from
                    // background. if we use two CTCallCenters, the calls that occurred in the background
                    // are passed to the second CTCallCenter. more on this here:
                    //
                    // http://stackoverflow.com/questions/21195732/how-to-get-a-call-event-using-ctcallcentersetcalleventhandler-that-occurred-wh
                    //
                    // since all call activity is logged via the second call center, just use it to gather call data.

                    _callCenter1 = new CTCallCenter();
                    _callCenter1.CallEventHandler += call =>
                    {
                        lock (_calls)
                            if (!ContainsCall(call))
                                _calls.Add(call);
                    };

                    _callCenter2 = new CTCallCenter();
                    _callCenter2.CallEventHandler += call =>
                    {
                        lock (_calls)
                            if (!ContainsCall(call))
                                _calls.Add(call);
                    };
                });
        }

        private bool ContainsCall(CTCall call)
        {
            lock (_calls)
                foreach (CTCall c in _calls)
                    if (c.CallID == call.CallID && c.CallState == call.CallState)
                        return true;

            return false;
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<TelephonyDatum> data = new List<TelephonyDatum>();

            lock (_calls)
            {
                foreach (CTCall call in _calls)
                {
                    TelephonyState state;
                    if (call.CallState == call.StateDialing)
                        state = TelephonyState.OutgoingCall;
                    else if (call.CallState == call.StateIncoming)
                        state = TelephonyState.IncomingCall;
                    else if (call.CallState == call.StateDisconnected)
                        state = TelephonyState.Idle;
                    else
                        continue;
                
                    data.Add(new TelephonyDatum(DateTimeOffset.Now, state, "", 0));
                }

                _calls.Clear();
            }

            // if we didn't find any calls, return a null to indicate that the poll went through but didn't find anything.
            if (data.Count == 0)
                data.Add(null);

            return data;
        }
    }
}