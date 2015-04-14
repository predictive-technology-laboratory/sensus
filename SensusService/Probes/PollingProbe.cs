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

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        private int _pollingSleepDurationMS;
        private bool _isPolling;
        private string _pollCallbackId;

        private readonly object _locker = new object();

        [EntryIntegerUiProperty("Sleep Duration:", true, 5)]
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

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
            _isPolling = false;
            _pollCallbackId = null;
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
                #elif __WINDOWS_PHONE__
                TODO:  Should we use a message?
                #endif

                _pollCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(cancellationToken =>
                    {
                        if (Running)
                        {
                            _isPolling = true;

                            IEnumerable<Datum> data = null;
                            try
                            {
                                SensusServiceHelper.Get().Logger.Log("Polling.", LoggingLevel.Verbose, GetType());
                                data = Poll(cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to poll:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }

                            if (data != null)
                                foreach (Datum datum in data)
                                {
                                    if(cancellationToken.IsCancellationRequested)
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
                    }, 0, _pollingSleepDurationMS, userNotificationMessage);
            }
        }

        protected abstract IEnumerable<Datum> Poll(CancellationToken cancellationToken);

        public override void Stop()
        {
            lock (_locker)
            {
                base.Stop();

                SensusServiceHelper.Get().UnscheduleRepeatingCallbackAsync(_pollCallbackId);
                _pollCallbackId = null;
            }
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                double msElapsedSincePreviousStore = (DateTimeOffset.UtcNow - MostRecentStoreTimestamp).TotalMilliseconds;
                if (!_isPolling && msElapsedSincePreviousStore > (_pollingSleepDurationMS + 100))  // there's a small amount of latency between the trigger of a poll and setting _isPolling to true, leading to a race condition with the tester method that can result in warnings about polling delays. allow a small fudge factor to ignore these warnings.
                    warning += "Probe \"" + GetType().FullName + "\" has not stored data in " + msElapsedSincePreviousStore + "ms (polling delay = " + _pollingSleepDurationMS + "ms)." + Environment.NewLine;
            }

            return restart;
        }
    }
}
