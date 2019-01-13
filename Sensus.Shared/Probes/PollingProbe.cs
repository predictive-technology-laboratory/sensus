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
    /// Polling Probes are triggered at regular intervals. When triggered, Polling Probes ask the device (and perhaps the user) for some type of 
    /// information and store the resulting information in the <see cref="LocalDataStore"/>.
    /// 
    /// # Background Considerations
    /// On Android, all Polling Probes are able to periodically wake up in the background, take a reading, and allow the system to go back to 
    /// sleep. The Android operating system will occasionally delay the wake-up signal in order to batch wake-ups and thereby conserve energy; however, 
    /// this delay is usually only 5-10 seconds. So, if you configure a Polling Probe to poll every 60 seconds, you may see actual polling delays of 
    /// 65-70 seconds and maybe even more. This is by design within Android and cannot be changed.
    /// 
    /// Polling on iOS is generally less reliable than on Android. By design, iOS apps are restricted from performing processing in the background, 
    /// with the following exceptions for <see cref="PollingProbe"/>s:
    /// 
    ///   * Significant location change processing:  If SignificantChangePoll is enabled, the Polling Probe will wake up each time
    ///     the user's physical location changes significantly. This change is triggered by a change in cellular tower, which is roughly on the 
    ///     order of several kilometers.
    /// 
    ///   * Push notification processing:  If you [configure push notifications](xref:push_notifications), the Polling Probe will be woken up
    ///     at the desired time to take a reading. Note that the reliability of these timings is subject to push notification throttling imposed
    ///     by the Apple Push Notification Service. The value of <see cref="PollingSleepDurationMS"/> should be set conservatively for all probes,
    ///     for example no lower than 15-20 minutes.
    /// 
    /// Beyond these exceptions, all processing within Sensus for iOS must be halted when the user backgrounds the app. Sensus does its best to support Polling 
    /// Probes on iOS by scheduling notifications to appear when polling operations (e.g., taking a GPS reading) should execute. This relies on the 
    /// user to open the notification from the tray and bring Sensus to the foreground so that the polling operation can execute. Of course, the user 
    /// might not see the notification or might choose not to open it. The polling operation will not be executed in such cases.
    /// </summary>
    public abstract class PollingProbe : Probe
    {
        private int _pollingSleepDurationMS;
        private int _pollingTimeoutMinutes;
        private bool _isPolling;
        private List<DateTime> _pollTimes;
        private ScheduledCallback _pollCallback;
        private bool _acPowerConnectPoll;
        private bool _acPowerConnectPollOverridesScheduledPolls;
        private EventHandler<bool> _powerConnectionChanged;

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

        /// <summary>
        /// Whether to poll on when the device is connected to AC Power.
        /// </summary>
        /// <value><c>true</c> if we should poll on power connect; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Poll On AC Power Connection:", true, 7)]
        public bool AcPowerConnectPoll
        {
            get { return _acPowerConnectPoll; }
            set { _acPowerConnectPoll = value; }
        }

        /// <summary>
        /// Has no effect if <see cref="AcPowerConnectPoll"/> is disabled. If <see cref="AcPowerConnectPoll"/> is enabled:  (1) If this 
        /// is on, polling will only occur on AC power connect. (2) If this is off, polling will occur based on <see cref="PollingSleepDurationMS"/> and 
        /// on AC power connect.
        /// </summary>
        /// <value><c>true</c> if AC power connect poll overrides scheduled polls; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("AC Power Connection Poll Overrides Scheduled Polls:", true, 8)]
        public bool AcPowerConnectPollOverridesScheduledPolls
        {
            get { return _acPowerConnectPollOverridesScheduledPolls; }
            set { _acPowerConnectPollOverridesScheduledPolls = value; }
        }

#if __IOS__
        /// <summary>
        /// Available on iOS only. Whether or not to poll when a significant change in location has occurred. See 
        /// [here](https://developer.apple.com/library/content/documentation/UserExperience/Conceptual/LocationAwarenessPG/CoreLocation/CoreLocation.html) for 
        /// more information on significant changes.
        /// </summary>
        /// <value><c>true</c> if significant change poll; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Poll On Significant Location Change:", true, 9)]
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
        [OnOffUiProperty("Significant Change Poll Overrides Scheduled Polls:", true, 10)]
        public bool SignificantChangePollOverridesScheduledPolls
        {
            get { return _significantChangePollOverridesScheduledPolls; }
            set { _significantChangePollOverridesScheduledPolls = value; }
        }
#endif

        /// <summary>
        /// Tolerance in milliseconds for running the <see cref="PollingProbe"/> before the scheduled 
        /// time, if doing so will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        [EntryIntegerUiProperty("Delay Tolerance Before (MS):", true, 11, true)]
        public int DelayToleranceBeforeMS { get; set; }

        /// <summary>
        /// Tolerance in milliseconds for running the <see cref="PollingProbe"/> after the scheduled 
        /// time, if doing so will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        [EntryIntegerUiProperty("Delay Tolerance After (MS):", true, 12, true)]
        public int DelayToleranceAfterMS { get; set; }

        public override string CollectionDescription
        {
            get
            {
                string description = DisplayName + ":  ";

                bool scheduledPollOverridden = false;

#if __IOS__
                if (_significantChangePoll)
                {
                    description += "On significant changes in the device's location. ";

                    if (_significantChangePollOverridesScheduledPolls)
                    {
                        scheduledPollOverridden = true;
                    }
                }
#endif

                if (_acPowerConnectPoll)
                {
                    description += "On AC power connection. ";

                    if (_acPowerConnectPollOverridesScheduledPolls)
                    {
                        scheduledPollOverridden = true;
                    }
                }

                if (!scheduledPollOverridden)
                {
                    description += TimeSpan.FromMilliseconds(_pollingSleepDurationMS).GetIntervalString();
                }

                return description;
            }
        }

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
            _pollingTimeoutMinutes = 5;
            _isPolling = false;
            _pollTimes = new List<DateTime>();
            _acPowerConnectPoll = false;
            _acPowerConnectPollOverridesScheduledPolls = false;

#if __IOS__
            _significantChangePoll = false;
            _significantChangePollOverridesScheduledPolls = false;
            _locationManager = new CLLocationManager();
            _locationManager.LocationsUpdated += async (sender, e) =>
            {
                try
                {
                    CancellationTokenSource pollCallbackCanceller = new CancellationTokenSource();

                    // if the callback specified a timeout, request cancellation at the specified time.
                    if (_pollCallback.Timeout.HasValue)
                    {
                        pollCallbackCanceller.CancelAfter(_pollCallback.Timeout.Value);
                    }   

                    await _pollCallback.ActionAsync(_pollCallback.Id, pollCallbackCanceller.Token, () => { });
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed significant change poll.", ex);
                }
            };
#endif

            _powerConnectionChanged = async (sender, connected) =>
            {
                try
                {
                    if (connected)
                    {
                        CancellationTokenSource pollCallbackCanceller = new CancellationTokenSource();

                        // if the callback specified a timeout, request cancellation at the specified time.
                        if (_pollCallback.Timeout.HasValue)
                        {
                            pollCallbackCanceller.CancelAfter(_pollCallback.Timeout.Value);
                        }

                        await _pollCallback.ActionAsync(_pollCallback.Id, pollCallbackCanceller.Token, () => { });
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed AC power connected poll:  " + ex.Message, ex);
                }
            };
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

            // we used to use an initial delay of zero in order to poll immediately; however, this causes the following
            // problems:
            // 
            //   * slow startup:  the immediate poll causes delays when starting the protocol. we show a progress page
            //                    when starting, but it's still irritating to the user.
            //
            //   * protocol restart timeout on push notification (ios):  ios occasionally kills the app, and we can wake
            //     it back up in the background via push notification. however, we only have ~30 seconds to finish processing
            //     the push notification before the system suspends the app. furthermore, long-running push notifications
            //     likely result in subsequent throttling of push notification delivery. 
            //
            // given the above, we now use an initial delay equal to the standard delay. the only cost is a single lost
            // reading at the very beginning.
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

            }, TimeSpan.FromMilliseconds(_pollingSleepDurationMS), TimeSpan.FromMilliseconds(_pollingSleepDurationMS), GetType().FullName, Protocol.Id, Protocol, TimeSpan.FromMinutes(_pollingTimeoutMinutes), userNotificationMessage, TimeSpan.FromMilliseconds(DelayToleranceBeforeMS), TimeSpan.FromMilliseconds(DelayToleranceAfterMS));

            bool schedulePollCallback = true;

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

                    if (_significantChangePollOverridesScheduledPolls)
                    {
                        schedulePollCallback = false;
                    }
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Location services not enabled.", LoggingLevel.Normal, GetType());
                }
            }
#endif       

            if (_acPowerConnectPoll)
            {
                SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged += _powerConnectionChanged;

                if (_acPowerConnectPollOverridesScheduledPolls)
                {
                    schedulePollCallback = false;
                }
            }

            if (schedulePollCallback)
            {
                await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(_pollCallback);
            }
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

            if (_acPowerConnectPoll)
            {
#pragma warning disable RECS0020 // Delegate subtraction has unpredictable result
                SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged -= _powerConnectionChanged;
#pragma warning restore RECS0020 // Delegate subtraction has unpredictable result
            }

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

                // don't check for polling delays if the scheduled polls are overridden.
                if (_acPowerConnectPoll && _acPowerConnectPollOverridesScheduledPolls)
                {
                    return result;
                }

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