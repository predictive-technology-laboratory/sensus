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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sensus.UI.UiProperties;
using Newtonsoft.Json;
using Sensus.Context;
using Sensus.Callbacks;
using Microsoft.AppCenter.Analytics;
using Sensus.Extensions;
using Sensus.Exceptions;

#if __IOS__
using CoreLocation;
#endif

namespace Sensus.Probes
{
    /// <summary>
    /// 
    /// Polling Probes are triggered at regular intervals. When triggered, Polling Probes ask the device (and perhaps the user) for some type of 
    /// information and store the resulting information in the <see cref="LocalDataStore"/>.
    /// 
    /// # Background Considerations
    /// On Android, all Polling Probes are able to periodically wake up in the background, take a reading, and allow the system to go back to 
    /// sleep. The Android operating system will occasionally delay the wake-up signal in order to batch wake-ups and thereby conserve energy; however, 
    /// this delay is usually only 5-10 seconds. So, if you configure a Polling Probe to poll every 60 seconds, you may see actual polling delays of 
    /// 65-70 seconds and maybe even more. This is by design within Android and cannot be changed.
    /// 
    /// Polling on iOS is much less reliable. By design, iOS apps cannot perform processing in the background, with the exception of 
    /// <see cref="Location.ListeningLocationProbe"/>. All other processing within Sensus must be halted when the user backgrounds the app. Furthermore, 
    /// Sensus cannot wake itself up from the background in order to execute polling operations. Thus, Sensus has no reliable mechanism to support polling-style
    /// operations. Sensus does its best to support Polling Probes on iOS by scheduling notifications to appear when polling operations (e.g., taking 
    /// a GPS reading) should execute. This relies on the user to open the notification from the tray and bring Sensus to the foreground so that the polling 
    /// operation can execute. Of course, the user might not see the notification or might choose not to open it. The polling operation will not be executed
    /// in such cases. You should assume that Polling Probes will not produce data reliably on iOS.
    /// 
    /// </summary>
    public abstract class PollingProbe : Probe
    {
        private int _pollingSleepDurationMS;
        private int _pollingTimeoutMinutes;
        private bool _isPolling;
        private List<DateTime> _pollTimes;
        private ScheduledCallback _pollCallback;

#if __IOS__
        private bool _significantChangePoll;
        private bool _significantChangePollOverridesScheduledPolls;
        private CLLocationManager _locationManager;
#endif

        /// <summary>
        /// How long to sleep (become inactive) between successive polling operations.
        /// </summary>
        /// <value>The polling sleep duration in milliseconds.</value>
        [EntryIntegerUiProperty("Sleep Duration (MS):", true, 5, true)]
        public virtual int PollingSleepDurationMS
        {
            get { return _pollingSleepDurationMS; }
            set
            {
                // we set this the same as CALLBACK_NOTIFICATION_HORIZON_THRESHOLD
                if (value <= 5000)
                {
                    value = 5000;
                }

                _pollingSleepDurationMS = value;
            }
        }

        /// <summary>
        /// How long the <see cref="PollingProbe"/>  has to complete a single poll operation before being cancelled.
        /// </summary>
        /// <value>The polling timeout minutes.</value>
        [EntryIntegerUiProperty("Timeout (Mins.):", true, 6, true)]
        public int PollingTimeoutMinutes
        {
            get
            {
                return _pollingTimeoutMinutes;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }

                _pollingTimeoutMinutes = value;
            }
        }

        [JsonIgnore]
        public abstract int DefaultPollingSleepDurationMS { get; }

        protected override double RawParticipation
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

        protected override long DataRateSampleSize => 10;

        public override double? MaxDataStoresPerSecond { get => null; set { } }

        public List<DateTime> PollTimes
        {
            get { return _pollTimes; }
        }

#if __IOS__
        /// <summary>
        /// Available on iOS only. Whether or not to poll when a significant change in location has occurred. See 
        /// [here](https://developer.apple.com/library/content/documentation/UserExperience/Conceptual/LocationAwarenessPG/CoreLocation/CoreLocation.html) for 
        /// more information on significant changes.
        /// </summary>
        /// <value><c>true</c> if significant change poll; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Significant Change Poll:", true, 7)]
        public bool SignificantChangePoll
        {
            get { return _significantChangePoll; }
            set { _significantChangePoll = value; }
        }

        /// <summary>
        /// Available on iOS only. Has no effect if significant-change polling is disabled. If significant-change polling is enabled:  (1) If this 
        /// is on, polling will only occur on significant changes. (2) If this is off, polling will occur based on <see cref="PollingSleepDurationMS"/> and 
        /// on significant changes.
        /// </summary>
        /// <value><c>true</c> if significant change poll overrides scheduled polls; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Significant Change Poll Overrides Scheduled Polls:", true, 8)]
        public bool SignificantChangePollOverridesScheduledPolls
        {
            get { return _significantChangePollOverridesScheduledPolls; }
            set { _significantChangePollOverridesScheduledPolls = value; }
        }
#endif

        public override string CollectionDescription
        {
            get
            {

#if __IOS__
                string significantChangeDescription = null;
                if (_significantChangePoll)
                {
                    significantChangeDescription = "On significant changes in the device's location";

                    if (_significantChangePollOverridesScheduledPolls)
                    {
                        return DisplayName + ":  " + significantChangeDescription + ".";
                    }
                }
#endif

                string intervalStr = TimeSpan.FromMilliseconds(_pollingSleepDurationMS).GetIntervalString();

#if __IOS__
                if (_significantChangePoll)
                {
                    intervalStr = significantChangeDescription + "; and " + intervalStr.ToLower();
                }
#endif

                return DisplayName + ":  " + intervalStr;
            }
        }

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
            _pollingTimeoutMinutes = 5;
            _isPolling = false;
            _pollTimes = new List<DateTime>();

#if __IOS__
            _significantChangePoll = false;
            _significantChangePollOverridesScheduledPolls = false;
            _locationManager = new CLLocationManager();
            _locationManager.LocationsUpdated += async (sender, e) =>
            {
                try
                {
                    CancellationTokenSource canceller = new CancellationTokenSource();

                    // if the callback specified a timeout, request cancellation at the specified time.
                    if (_pollCallback.CallbackTimeout.HasValue)
                    {
                        canceller.CancelAfter(_pollCallback.CallbackTimeout.Value);
                    }

                    await _pollCallback.Action(_pollCallback.Id, canceller.Token, () => { });
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed significant change poll.", ex);
                }
            };
#endif

        }

        protected override async Task ProtectedStartAsync()
        {
            await base.ProtectedStartAsync();

#if __IOS__
            string userNotificationMessage = DisplayName + " data requested.";
#elif __ANDROID__
            string userNotificationMessage = null;
#elif LOCAL_TESTS
            string userNotificationMessage = null;
#else
#warning "Unrecognized platform"
            string userNotificationMessage = null;
#endif

            _pollCallback = new ScheduledCallback(async (callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                if (Running)
                {
                    _isPolling = true;

                    List<Datum> data = null;
                    try
                    {
                        SensusServiceHelper.Get().Logger.Log("Polling.", LoggingLevel.Normal, GetType());
                        data = await PollAsync(cancellationToken);

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
                    {
                        foreach (Datum datum in data)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            try
                            {
                                await StoreDatumAsync(datum, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to store datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                        }
                    }

                    _isPolling = false;
                }

            }, TimeSpan.Zero, TimeSpan.FromMilliseconds(_pollingSleepDurationMS), GetType().FullName, Protocol.Id, Protocol, TimeSpan.FromMinutes(_pollingTimeoutMinutes), userNotificationMessage);

#if __IOS__

            if (_significantChangePoll)
            {
                _locationManager.RequestAlwaysAuthorization();
                _locationManager.DistanceFilter = 5.0;
                _locationManager.PausesLocationUpdatesAutomatically = false;
                _locationManager.AllowsBackgroundLocationUpdates = true;

                if (CLLocationManager.LocationServicesEnabled)
                {
                   _locationManager.StartMonitoringSignificantLocationChanges();
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Location services not enabled.", LoggingLevel.Normal, GetType());
                }
            }

            // schedule the callback if we're not doing significant-change polling, or if we are but the latter doesn't override the former.
            if (!_significantChangePoll || !_significantChangePollOverridesScheduledPolls)  
            {
                await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(_pollCallback);
            }

#elif __ANDROID__
            await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(_pollCallback);
#endif
        }

        protected abstract Task<List<Datum>> PollAsync(CancellationToken cancellationToken);

        public override async Task StopAsync()
        {
            await base.StopAsync();

#if __IOS__
            if (_significantChangePoll)
            {
                _locationManager.StopMonitoringSignificantLocationChanges();
            }
#endif

            await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(_pollCallback);
            _pollCallback = null;
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            if (Running)
            {

#if __IOS__
                // on ios we do significant-change polling, which can override scheduled polls. don't check for polling delays if the scheduled polls are overridden.
                if (_significantChangePoll && _significantChangePollOverridesScheduledPolls)
                {
                    return result;
                }
#endif

                TimeSpan timeElapsedSincePreviousStore = DateTimeOffset.UtcNow - MostRecentStoreTimestamp.GetValueOrDefault(DateTimeOffset.MinValue);
                int allowedLagMS = 5000;
                if (!_isPolling &&                                                                               // don't raise a warning if the probe is currently trying to poll
                    _pollingSleepDurationMS <= int.MaxValue - allowedLagMS &&                                    // some probes (iOS HealthKit for age) have polling delays set to int.MaxValue. if we add to this (as we're about to do in the next check), we'll wrap around to 0 resulting in incorrect statuses. only do the check if we won't wrap around.
                    timeElapsedSincePreviousStore.TotalMilliseconds > (_pollingSleepDurationMS + allowedLagMS))  // system timer callbacks aren't always fired exactly as scheduled, resulting in health tests that identify warning conditions for delayed polling. allow a small fudge factor to ignore these warnings.
                {
                    string eventName = TrackedEvent.Warning + ":" + GetType().Name;
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "Polling Latency", (timeElapsedSincePreviousStore.TotalMilliseconds - _pollingSleepDurationMS).RoundToWhole(1000).ToString() }
                    };

                    Analytics.TrackEvent(eventName, properties);

                    events.Add(new AnalyticsTrackedEvent(eventName, properties));
                }

                if (!SensusContext.Current.CallbackScheduler.ContainsCallback(_pollCallback))
                {
                    string eventName = TrackedEvent.Error + ":" + GetType().Name;
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "Missing Callback", _pollCallback.Id }
                    };

                    Analytics.TrackEvent(eventName, properties);

                    events.Add(new AnalyticsTrackedEvent(eventName, properties));

                    result = HealthTestResult.Restart;
                }
            }

            return result;
        }

        public override async Task ResetAsync()
        {
            await base.ResetAsync();

            _isPolling = false;
            _pollCallback = null;

            lock (_pollTimes)
            {
                _pollTimes.Clear();
            }
        }
    }
}