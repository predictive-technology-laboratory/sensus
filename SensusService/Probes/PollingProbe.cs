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

using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using System.Linq;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        private int _pollingSleepDurationMS;
        private bool _isPolling;
        private string _pollCallbackId;
        private List<DateTime> _pollTimes;

        private readonly object _locker = new object();

        [EntryIntegerUiProperty("Sleep Duration (MS):", true, 5)]
        public virtual int PollingSleepDurationMS
        {
            get { return _pollingSleepDurationMS; }
            set
            {
                if (value <= 1000)
                    value = 1000;
                
                if (value != _pollingSleepDurationMS)
                {
                    _pollingSleepDurationMS = value; 

                    if (_pollCallbackId != null)
                        _pollCallbackId = SensusServiceHelper.Get().RescheduleRepeatingCallback(_pollCallbackId, _pollingSleepDurationMS, _pollingSleepDurationMS);
                }
            }
        }

        [JsonIgnore]
        public abstract int DefaultPollingSleepDurationMS { get; }

        protected override float RawParticipation
        {
            get
            {
                int oneDayMS = (int)new TimeSpan(1, 0, 0, 0).TotalMilliseconds;
                float pollsPerDay = oneDayMS / (float)_pollingSleepDurationMS;
                float fullParticipationPolls = pollsPerDay * Protocol.ParticipationHorizonDays;
                return _pollTimes.Count(pollTime => pollTime >= Protocol.ParticipationHorizon) / fullParticipationPolls;
            }
        }

        public List<DateTime> PollTimes
        {
            get { return _pollTimes; }
        }

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
            _isPolling = false;
            _pollCallbackId = null;
            _pollTimes = new List<DateTime>();
        }

        /// <summary>
        /// Starts this probe. Throws an exception if start fails.
        /// </summary>
        public override void Start()
        {
            lock (_locker)
            {
                base.Start();

                #if __IOS__
                string userNotificationMessage = DisplayName + " data requested.";
                #elif __ANDROID__
                string userNotificationMessage = null;
                #elif WINDOWS_PHONE
                string userNotificationMessage = null; // TODO:  Should we use a message?
                #else
                #error "Unrecognized platform."
                #endif

                _pollCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback((callbackId, cancellationToken) =>
                    {
                        if (Running)
                        {
                            _isPolling = true;

                            IEnumerable<Datum> data = null;
                            try
                            {
                                SensusServiceHelper.Get().Logger.Log("Polling.", LoggingLevel.Normal, GetType());
                                data = Poll(cancellationToken);
                                _pollTimes.Add(DateTime.Now);
                                _pollTimes.RemoveAll(pollTime => pollTime < Protocol.ParticipationHorizon);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to poll:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }

                            if (data != null)
                                foreach (Datum datum in data)
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                        break;
                                    
                                    try
                                    {
                                        StoreDatum(datum);
                                    }
                                    catch (Exception ex)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Failed to store datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                                    }
                                }

                            _isPolling = false;
                        }
                    }, GetType().FullName + " Poll", 0, _pollingSleepDurationMS, userNotificationMessage);
            }
        }

        protected abstract IEnumerable<Datum> Poll(CancellationToken cancellationToken);

        public override void Stop()
        {
            lock (_locker)
            {
                base.Stop();

                SensusServiceHelper.Get().UnscheduleRepeatingCallback(_pollCallbackId);
                _pollCallbackId = null;
            }
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                double msElapsedSincePreviousStore = (DateTimeOffset.UtcNow - MostRecentStoreTimestamp).TotalMilliseconds;
                int allowedLagMS = 5000;
                if (!_isPolling &&
                    _pollingSleepDurationMS <= int.MaxValue - allowedLagMS && // some probes (iOS HealthKit) have polling delays set to int.MaxValue. if we add to this (as we're about to do in the next check), we'll wrap around to 0 resulting in incorrect statuses. only do the check if we won't wrap around.
                    msElapsedSincePreviousStore > (_pollingSleepDurationMS + allowedLagMS))  // system timer callbacks aren't always fired exactly as scheduled, resulting in health tests that identify warning conditions for delayed polling. allow a small fudge factor to ignore these warnings.
                    warning += "Probe \"" + GetType().FullName + "\" has not stored data in " + msElapsedSincePreviousStore + "ms (polling delay = " + _pollingSleepDurationMS + "ms)." + Environment.NewLine;
            }

            return restart;
        }

        public override void ResetForSharing()
        {
            base.ResetForSharing();

            _isPolling = false;
            _pollCallbackId = null;
            _pollTimes.Clear();
        }
    }
}
