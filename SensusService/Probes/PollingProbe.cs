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
using Newtonsoft.Json;


#endregion

using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        private int _pollingSleepDurationMS;
        private Thread _pollThread;
        private bool _isPolling;

        [EntryIntegerUiProperty("Sleep Duration:", true, 5)]
        public virtual int PollingSleepDurationMS
        {
            get { return _pollingSleepDurationMS; }
            set { _pollingSleepDurationMS = value; }
        }

        [JsonIgnore]
        public abstract int DefaultPollingSleepDurationMS { get; }

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
            _isPolling = false;
        }

        /// <summary>
        /// Starts this probe. Throws an exception if start fails.
        /// </summary>
        public override void Start()
        {
            lock (this)
            {
                base.Start();

                _pollThread = new Thread(() =>
                    {
                        PollingStarted();

                        int msToSleep = 0;  // poll immediately the first time

                        while (Running)
                        {
                            // in order to allow the commit thread to be interrupted by Stop, sleep for 1-second intervals.
                            Thread.Sleep(1000);
                            msToSleep -= 1000;

                            // have we slept enough to run a poll? if not, continue the loop.
                            if (msToSleep > 0)
                                continue;

                            // if we're still running, execute a poll.
                            if (Running)
                            {
                                _isPolling = true;

                                IEnumerable<Datum> data = null;
                                try { data = Poll(); }
                                catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to poll probe \"" + GetType().FullName + "\":  " + ex.Message, LoggingLevel.Normal); }

                                if (data != null)
                                    foreach (Datum datum in data)
                                        try { StoreDatum(datum); }
                                        catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to store datum in probe \"" + GetType().FullName + "\":  " + ex.Message, LoggingLevel.Normal); }

                                _isPolling = false;
                            }

                            msToSleep = _pollingSleepDurationMS;
                        }

                        PollingStopped();
                    });

                _pollThread.Start();
            }
        }

        protected virtual void PollingStarted() { }

        protected abstract IEnumerable<Datum> Poll();

        public override void Stop()
        {
            lock (this)
            {
                base.Stop();

                // since Running is now false, the poll thread will be exiting soon. if it's in the middle of a poll, the poll will finish.
                if (_pollThread != null)
                    _pollThread.Join();
            }
        }

        protected virtual void PollingStopped() { }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                DateTimeOffset mostRecentDatumTimestamp = DateTimeOffset.MinValue;
                if (MostRecentDatum != null)
                    mostRecentDatumTimestamp = MostRecentDatum.Timestamp;

                double msElapsedSinceLastDatum = (DateTimeOffset.UtcNow - mostRecentDatumTimestamp).TotalMilliseconds;
                if (!_isPolling && msElapsedSinceLastDatum > _pollingSleepDurationMS)
                    warning += "Probe \"" + GetType().FullName + "\" has not taken a reading in " + msElapsedSinceLastDatum + "ms (polling delay = " + _pollingSleepDurationMS + "ms)." + Environment.NewLine;
            }

            return restart;
        }
    }
}
