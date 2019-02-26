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

using Sensus.UI.UiProperties;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using Sensus.Callbacks;
using Sensus.Context;
using System.IO;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;
using Sensus.Extensions;
using Sensus.Notifications;

namespace Sensus.DataStores.Remote
{
    /// <summary>
    /// 
    /// A Remote Data Store periodically transfers data from the device's <see cref="Local.LocalDataStore"/> to a remote storage system 
    /// (e.g., Amazon's Simple Storage Service). The job of the Remote Data Store is to ensure that data accumulated locally on the device 
    /// are safely transferred off of the device before their accumulated size grows too large or they are corrupted, deleted, lost, etc. 
    /// 
    /// </summary>
    public abstract class RemoteDataStore : DataStore
    {
        private int _writeDelayMS;
        private int _writeTimeoutMinutes;
        private DateTime? _mostRecentSuccessfulWriteTime;
        private ScheduledCallback _writeCallback;
        private bool _writeOnPowerConnect;
        private bool _requireWiFi;
        private bool _requireCharging;
        private float _requiredBatteryChargeLevelPercent;
        private string _userNotificationMessage;
        private EventHandler<bool> _powerConnectionChanged;
        private CancellationTokenSource _powerConnectWriteCancellationToken;

        /// <summary>
        /// How many milliseconds to pause between each data write cycle.
        /// </summary>
        /// <value>The write delay in milliseconds.</value>
        [EntryIntegerUiProperty("Write Delay (MS):", true, 50, true)]
        public int WriteDelayMS
        {
            get { return _writeDelayMS; }
            set
            {
                if (value <= 1000)
                {
                    value = 1000;
                }

                _writeDelayMS = value;
            }
        }

        /// <summary>
        /// How many minutes the data store has to complete a write before being cancelled.
        /// </summary>
        /// <value>The write timeout in minutes.</value>
        [EntryIntegerUiProperty("Write Timeout (Mins.):", true, 51, true)]
        public int WriteTimeoutMinutes
        {
            get
            {
                return _writeTimeoutMinutes;
            }
            set
            {
                if (value <= 0)
                {
                    value = 1;
                }

                _writeTimeoutMinutes = value;
            }
        }

        /// <summary>
        /// Whether to initiate an additional data upload each time the device is plugged in to external 
        /// power. These additional uploads will respect the other settings (e.g., <see cref="RequireWiFi"/>).
        /// </summary>
        /// <value><c>true</c> to upload when plugged in, otherwise <c>false</c>.</value>
        [OnOffUiProperty("Write on Power Connect:", true, 52)]
        public bool WriteOnPowerConnect
        {
            get { return _writeOnPowerConnect; }
            set { _writeOnPowerConnect = value; }
        }

        /// <summary>
        /// Whether to require a WiFi connection when uploading data. If this is turned off, substantial data charges might result since 
        /// data will be transferred over the cellular network if WiFi is not available.
        /// </summary>
        /// <value><c>true</c> if WiFi is required; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Require WiFi:", true, 53)]
        public bool RequireWiFi
        {
            get { return _requireWiFi; }
            set { _requireWiFi = value; }
        }

        /// <summary>
        /// Whether to require external power when uploading data.
        /// </summary>
        /// <value><c>true</c> to require charging; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Require Charging:", true, 54)]
        public bool RequireCharging
        {
            get { return _requireCharging; }
            set { _requireCharging = value; }
        }

        /// <summary>
        /// The battery charge percent required to write data to the <see cref="RemoteDataStore"/>. This value is 
        /// only considered when <see cref="RequireCharging"/> is <c>false</c>. Since remote data storage relies on wireless
        /// data transmission, significant battery consumption may result. This value provides flexibility when combined with 
        /// <see cref="RequireCharging"/>. For example, when <see cref="RequireCharging"/> is <c>false</c> and 
        /// <see cref="RequiredBatteryChargeLevelPercent"/> is 95%, Sensus will be allowed to transfer data without an external
        /// power source only when the battery is almost fully charged. This will minimize user irritation.
        /// </summary>
        /// <value>The required battery charge level percent.</value>
        [EntryFloatUiProperty("Required Battery Charge (Percent):", true, 55, false)]
        public float RequiredBatteryChargeLevelPercent
        {
            get { return _requiredBatteryChargeLevelPercent; }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                _requiredBatteryChargeLevelPercent = value;
            }
        }

        /// <summary>
        /// Available on iOS only. The message displayed to the user when Sensus is in the background and data are 
        /// scheduled to be transferred to the <see cref="RemoteDataStore"/>. This message is delivered via a notification 
        /// in the hope that the user will open Sensus to transmit the data. Data will not be transferred if the user
        /// does not open Sensus.
        /// </summary>
        /// <value>The user notification message.</value>
        [EntryStringUiProperty("(iOS) User Notification Message:", true, 56, true)]
        public string UserNotificationMessage
        {
            get { return _userNotificationMessage; }
            set { _userNotificationMessage = value; }
        }

        /// <summary>
        /// Tolerance in milliseconds for running the <see cref="RemoteDataStore"/> before the scheduled 
        /// time, if doing so will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        [EntryIntegerUiProperty("Delay Tolerance Before (MS):", true, 60, true)]
        public int DelayToleranceBeforeMS { get; set; }

        /// <summary>
        /// Tolerance in milliseconds for running the <see cref="RemoteDataStore"/> after the scheduled 
        /// time, if doing so will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        [EntryIntegerUiProperty("Delay Tolerance After (MS):", true, 61, true)]
        public int DelayToleranceAfterMS { get; set; }

        [JsonIgnore]
        public abstract bool CanRetrieveWrittenData { get; }

        [JsonIgnore]
        public virtual string StorageDescription
        {
            get
            {
                if (!Protocol?.LocalDataStore?.WriteToRemote ?? false)
                {
                    return "All data will remain on this device. No data will be transmitted.";
                }
                else
                {
                    return null;
                }
            }
        }

        protected RemoteDataStore()
        {
            _writeTimeoutMinutes = 5;
            _mostRecentSuccessfulWriteTime = null;
            _writeOnPowerConnect = true;
            _requireWiFi = true;
            _requireCharging = true;
            _requiredBatteryChargeLevelPercent = 20;
            _userNotificationMessage = "Please open this notification to submit your data.";

#if DEBUG || UI_TESTING
            WriteDelayMS = 10000;  // 10 seconds...so we can see debugging output quickly
#else
            WriteDelayMS = 1000 * 60 * 60;  // every 60 minutes
#endif

            _powerConnectionChanged = async (sender, connected) =>
            {
                if (connected)
                {
                    if (_writeOnPowerConnect)
                    {
                        SensusServiceHelper.Get().Logger.Log("Writing to remote on power connect signal.", LoggingLevel.Normal, GetType());
                        _powerConnectWriteCancellationToken = new CancellationTokenSource();
                        await WriteLocalDataStoreAsync(_powerConnectWriteCancellationToken.Token);
                    }
                }
                else
                {
                    // cancel any prior write attempts resulting from AC power connection
                    _powerConnectWriteCancellationToken?.Cancel();
                }
            };
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();

            _mostRecentSuccessfulWriteTime = DateTime.Now;

            string userNotificationMessage = null;

#if __IOS__
            // we can't wake up the app on ios. this is problematic since data need to be stored locally and remotely
            // in something of a reliable schedule; otherwise, we risk data loss (e.g., from device restarts, app kills, etc.).
            // so, do the best possible thing and bug the user with a notification indicating that data need to be stored.
            userNotificationMessage = _userNotificationMessage;
#endif

            _writeCallback = new ScheduledCallback(cancellationToken => WriteLocalDataStoreAsync(cancellationToken), TimeSpan.FromMilliseconds(_writeDelayMS), TimeSpan.FromMilliseconds(_writeDelayMS), GetType().FullName, Protocol.Id, Protocol, TimeSpan.FromMinutes(_writeTimeoutMinutes), userNotificationMessage, TimeSpan.FromMilliseconds(DelayToleranceBeforeMS), TimeSpan.FromMilliseconds(DelayToleranceAfterMS));
            await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(_writeCallback);

            // hook into the AC charge event signal -- add handler to AC broadcast receiver
            SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged += _powerConnectionChanged;
        }

        public override async Task StopAsync()
        {
            await base.StopAsync();

            await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(_writeCallback);

            // unhook from the AC charge event signal -- remove handler to AC broadcast receiver
            SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged -= _powerConnectionChanged;
        }

        public override void Reset()
        {
            base.Reset();

            _mostRecentSuccessfulWriteTime = null;
            _writeCallback = null;
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            if (_mostRecentSuccessfulWriteTime.HasValue)
            {
                TimeSpan timeElapsedSincePreviousWrite = DateTime.Now - _mostRecentSuccessfulWriteTime.Value;
                if (timeElapsedSincePreviousWrite.TotalMilliseconds > (_writeDelayMS + 5000))  // system timer callbacks aren't always fired exactly as scheduled, resulting in health tests that identify warning conditions for delayed data storage. allow a small fudge factor to ignore most of these warnings.
                {
                    string eventName = TrackedEvent.Warning + ":" + GetType().Name;
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "Storage Latency", (timeElapsedSincePreviousWrite.TotalMilliseconds).RoundToWhole(1000).ToString() }
                    };

                    Analytics.TrackEvent(eventName, properties);

                    events.Add(new AnalyticsTrackedEvent(eventName, properties));
                }
            }

            if (!SensusContext.Current.CallbackScheduler.ContainsCallback(_writeCallback))
            {
                string eventName = TrackedEvent.Error + ":" + GetType().Name;
                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    { "Missing Callback", _writeCallback.Id }
                };

                Analytics.TrackEvent(eventName, properties);

                events.Add(new AnalyticsTrackedEvent(eventName, properties));

                result = HealthTestResult.Restart;
            }

            return result;
        }

        public async Task<bool> WriteLocalDataStoreAsync(CancellationToken cancellationToken)
        {
            bool write = false;

            if (cancellationToken.IsCancellationRequested)
            {
                SensusServiceHelper.Get().Logger.Log("Cancelled write to remote data store via token.", LoggingLevel.Normal, GetType());
            }
            else if (_requireWiFi && !SensusServiceHelper.Get().WiFiConnected)
            {
                SensusServiceHelper.Get().Logger.Log("Required WiFi but device WiFi is not connected.", LoggingLevel.Normal, GetType());
            }
            else if (_requireCharging && !SensusServiceHelper.Get().IsCharging)
            {
                SensusServiceHelper.Get().Logger.Log("Required charging but device is not charging.", LoggingLevel.Normal, GetType());
            }
            else if (!_requireCharging && !SensusServiceHelper.Get().IsCharging && _requiredBatteryChargeLevelPercent > 0 && SensusServiceHelper.Get().BatteryChargePercent < _requiredBatteryChargeLevelPercent)
            {
                SensusServiceHelper.Get().Logger.Log("Charging not required, but the battery charge percent is lower than required.", LoggingLevel.Normal, GetType());
            }
            else
            {
                write = true;
            }

            if (write)
            {
                // instruct the local data store to write its data to the remote data store. catch any exceptions that result
                // as the caller may be on the app's main thread and we want to avoid crashing the app if the write times out
                // or raises some other exception.
                try
                {
                    await Protocol.LocalDataStore.WriteToRemoteAsync(cancellationToken);
                    _mostRecentSuccessfulWriteTime = DateTime.Now;
                    return true;
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to write local data store to remote:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the script agent policy from the <see cref="RemoteDataStore"/>. See concrete class implementation for details.
        /// </summary>
        /// <returns>The script agent policy.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task<string> GetScriptAgentPolicyAsync(CancellationToken cancellationToken);

        public abstract Task<string> GetProtocolUpdatesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Writes a stream of <see cref="Datum"/> objects.
        /// </summary>
        /// <returns>The stream.</returns>
        /// <param name="stream">Stream.</param>
        /// <param name="name">Descriptive name for the stream.</param>
        /// <param name="contentType">MIME content type for the stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task WriteDataStreamAsync(Stream stream, string name, string contentType, CancellationToken cancellationToken);

        /// <summary>
        /// Writes a single <see cref="Datum"/> to the <see cref="RemoteDataStore"/>.
        /// </summary>
        /// <returns>Task.</returns>
        /// <param name="datum">Datum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task WriteDatumAsync(Datum datum, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the push notification token.
        /// </summary>
        /// <returns>The push notification token.</returns>
        /// <param name="token">Token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task SendPushNotificationTokenAsync(string token, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the push notification token.
        /// </summary>
        /// <returns>The push notification token.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task DeletePushNotificationTokenAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the push notification request.
        /// </summary>
        /// <returns>The push notification request.</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task SendPushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the push notification request.
        /// </summary>
        /// <returns>Task</returns>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task DeletePushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the push notifiation request.
        /// </summary>
        /// <returns>Task</returns>
        /// <param name="id">Push notification request identifier (see <see cref="PushNotificationRequest.Id"/>).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task DeletePushNotificationRequestAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the key (identifier) value for a <see cref="Datum"/>. Used within <see cref="WriteDatumAsync"/> and <see cref="GetDatumAsync"/>.
        /// </summary>
        /// <returns>The datum key.</returns>
        /// <param name="datum">Datum.</param>
        public abstract string GetDatumKey(Datum datum);

        /// <summary>
        /// Retrieves a single <see cref="Datum"/> from the <see cref="RemoteDataStore"/>.
        /// </summary>
        /// <returns>The datum.</returns>
        /// <param name="datumKey">Datum key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The type of <see cref="Datum"/> to retrieve.</typeparam>
        public abstract Task<T> GetDatumAsync<T>(string datumKey, CancellationToken cancellationToken) where T : Datum;
    }
}