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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Band.Portable;
using Newtonsoft.Json;
using SensusService.Probes;
using System.Linq;
using System.Collections.Generic;

namespace SensusService
{
    public abstract class MicrosoftBandProbeBase : ListeningProbe
    {
        private const int HEALTH_TEST_DELAY_MS = 60000;
        private static string HEALTH_TEST_CALLBACK_ID;
        private static readonly object HEALTH_TEST_LOCKER = new object();

        private static List<MicrosoftBandProbeBase> BandProbesThatShouldBeRunning
        {
            get
            {
                return SensusServiceHelper.Get().GetRunningProtocols().SelectMany(protocol => protocol.Probes.Where(probe => probe.Enabled && probe is MicrosoftBandProbeBase)).Cast<MicrosoftBandProbeBase>().ToList();
            }
        }

        private static void CancelHealthTest()
        {
            lock (HEALTH_TEST_LOCKER)
            {
                if (HEALTH_TEST_CALLBACK_ID != null)
                {
                    SensusServiceHelper.Get().UnscheduleCallback(HEALTH_TEST_CALLBACK_ID);
                    HEALTH_TEST_CALLBACK_ID = null;
                }
            }
        }

        private BandClient _bandClient;

        [JsonIgnore]
        protected BandClient BandClient
        {
            get
            {
                return _bandClient;
            }
            set
            {
                _bandClient = value;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            // we expect this probe to start successfully, but an exception may occur if no bands are paired with the device of if the
            // connection with a paired band fails. so schedule a static repeating callback to check on all band probes and restart them 
            // if needed/possible. this is better than a non-static callback for each band probe because there are many band probes and 
            // the callbacks would be redundant, frequent, and power-hungry.
            lock (HEALTH_TEST_LOCKER)
            {
                // only schedule the callback if we haven't done so already. the callback should be global across all band probes.
                if (HEALTH_TEST_CALLBACK_ID == null)
                {
                    ScheduledCallback callback = new ScheduledCallback(TestBandClientsAsync, "Microsoft Band Health Test", TimeSpan.FromMinutes(5));
                    HEALTH_TEST_CALLBACK_ID = SensusServiceHelper.Get().ScheduleRepeatingCallback(callback, HEALTH_TEST_DELAY_MS, HEALTH_TEST_DELAY_MS, false);
                }
            }
        }

        protected override void StopListening()
        {
            // only cancel the static health test if none of the band probes should be running.
            if (BandProbesThatShouldBeRunning.Count == 0)
                CancelHealthTest();
        }

        public static Task TestBandClientsAsync(string callbackId, CancellationToken cancellationToken, Action letDeviceSleepCallback)
        {
            return Task.Run(() =>
            {
                List<MicrosoftBandProbeBase> bandProbesThatShouldBeRunning = BandProbesThatShouldBeRunning;

                // if no band probes should be running, then ignore the current test and unschedule the test callback.
                if (bandProbesThatShouldBeRunning.Count == 0)
                    CancelHealthTest();
                else
                {
                    foreach (MicrosoftBandProbeBase probe in bandProbesThatShouldBeRunning)
                    {
                        // restart if this probe is not running or if the band client is not connected
                        if (!probe.Running || probe.BandClient == null || !probe.BandClient.IsConnected)
                        {
                            try
                            {
                                probe.Restart();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            });
        }
    }
}