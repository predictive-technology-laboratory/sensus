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
using System.Threading.Tasks;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        /// <summary>
        /// It's important to mitigate lag in polling operations since participation assessments are done on the basis of poll rates.
        /// </summary>
        private const bool POLL_CALLBACK_LAG = false;

        private int _pollingSleepDurationMS;
        private int _pollingTimeoutMinutes;
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
                        _pollCallbackId = SensusServiceHelper.Get().RescheduleRepeatingCallback(_pollCallbackId, _pollingSleepDurationMS, _pollingSleepDurationMS, POLL_CALLBACK_LAG);
                }
            }
        }

        [EntryIntegerUiProperty("Timeout (Mins.):", true, 6)]
        public int PollingTimeoutMinutes
        {
            get
            {
                return _pollingTimeoutMinutes;
            }
            set
            {
                if (value < 1)
                    value = 1;

                _pollingTimeoutMinutes = value;
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

                lock (_pollTimes)
                {
                    return _pollTimes.Count(pollTime => pollTime >= Protocol.ParticipationHorizon) / fullParticipationPolls;
                }
            }
        }

        public List<DateTime> PollTimes
        {
            get { return _pollTimes; }
        }

        public override string CollectionDescription
        {
            get
            {
                TimeSpan interval = new TimeSpan(0, 0, 0, 0, _pollingSleepDurationMS);

                double value = -1;
                string unit;
                int decimalPlaces = 0;

                if (interval.TotalSeconds <= 60)
                {
                    value = interval.TotalSeconds;
                    unit = "second";
                    decimalPlaces = 1;
                }
                else if (interval.TotalMinutes <= 60)
                {
                    value = interval.TotalMinutes;
                    unit = "minute";
                }
                else if (interval.TotalHours <= 24)
                {
                    value = interval.TotalHours;
                    unit = "hour";
                }
                else
                {
                    value = interval.TotalDays;
                    unit = "day";
                }

                value = Math.Round(value, decimalPlaces);

                if (value == 1)
                    return DisplayName + ":  Once per " + unit + ".";
                else
                    return DisplayName + ":  Every " + value + " " + unit + "s.";
            }
        }

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
            _pollingTimeoutMinutes = 5;
            _isPolling = false;
            _pollCallbackId = null;
            _pollTimes = new List<DateTime>();
        }

        protected override void InternalStart()
        {
            lock (_locker)
            {
                base.InternalStart();

#if __IOS__
                string userNotificationMessage = DisplayName + " data requested.";
#elif __ANDROID__
                string userNotificationMessage = null;
#elif WINDOWS_PHONE
                string userNotificationMessage = null; // TODO:  Should we use a message?
#else
#error "Unrecognized platform."
#endif

                ScheduledCallback callback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) =>
                    {
                        return Task.Run(async () =>
                            {
                                if (Running)
                                {
                                    _isPolling = true;

                                    IEnumerable<Datum> data = null;
                                    try
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Polling.", LoggingLevel.Normal, GetType());
                                        data = Poll(cancellationToken);

                                        lock (_pollTimes)
                                        {
                                            _pollTimes.Add(DateTime.Now);
                                            _pollTimes.RemoveAll(pollTime => pollTime < Protocol.ParticipationHorizon);
                                        }
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
                                                await StoreDatumAsync(datum, cancellationToken);
                                            }
                                            catch (Exception ex)
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Failed to store datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                                            }
                                        }

                                    _isPolling = false;
                                }
                            });

                    }, GetType().FullName + " Poll", TimeSpan.FromMinutes(_pollingTimeoutMinutes), userNotificationMessage);

                _pollCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(callback, 0, _pollingSleepDurationMS, POLL_CALLBACK_LAG);
            }
        }

        protected abstract IEnumerable<Datum> Poll(CancellationToken cancellationToken);

        public override void Stop()
        {
            lock (_locker)
            {
                base.Stop();

                SensusServiceHelper.Get().UnscheduleCallback(_pollCallbackId);
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

            lock (_pollTimes)
            {
                _pollTimes.Clear();
            }
        }
    }
}
