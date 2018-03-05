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
        /// <summary>
        /// We don't mind write callback lags, since they don't affect any performance metrics and
        /// the latencies aren't inspected when testing data store health or participation. It also
        /// doesn't make sense to force rapid write since data will not have accumulated.
        /// </summary>
        private const bool WRITE_CALLBACK_LAG = true;

        private int _writeDelayMS;
        private int _writeTimeoutMinutes;
        private DateTime? _mostRecentSuccessfulWriteTime;
        private ScheduledCallback _writeCallback;
        private bool _requireWiFi;
        private bool _requireCharging;

        /// <summary>
        /// How many milliseconds to pause between each writing data.
        /// </summary>
        /// <value>The write delay in milliseconds.</value>
        [EntryIntegerUiProperty("Write Delay (MS):", true, 2)]
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
        [EntryIntegerUiProperty("Write Timeout (Mins.):", true, 3)]
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
        /// Whether to require a WiFi connection when uploading data. If this is turned off, substantial data charges might result since 
        /// data will be transferred over the cellular network if WiFi is not available.
        /// </summary>
        /// <value><c>true</c> if WiFi is required; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Require WiFi:", true, int.MaxValue)]
        public bool RequireWiFi
        {
            get { return _requireWiFi; }
            set { _requireWiFi = value; }
        }

        /// <summary>
        /// Whether to require external power when uploading data.
        /// </summary>
        /// <value><c>true</c> to require charging; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Require Charging:", true, int.MaxValue)]
        public bool RequireCharging
        {
            get { return _requireCharging; }
            set { _requireCharging = value; }
        }

        [JsonIgnore]
        public abstract bool CanRetrieveWrittenData { get; }

        protected RemoteDataStore()
        {
            _writeDelayMS = 10000;
            _writeTimeoutMinutes = 5;
            _mostRecentSuccessfulWriteTime = null;
            _requireWiFi = true;
            _requireCharging = true;

#if DEBUG || UI_TESTING
            WriteDelayMS = 10000;  // 10 seconds...so we can see debugging output quickly
#else
            WriteDelayMS = 1000 * 60 * 60;  // every 60 minutes
#endif
        }

        public override void Start()
        {
            base.Start();

            _mostRecentSuccessfulWriteTime = DateTime.Now;

            string userNotificationMessage = null;

#if __IOS__
            // we can't wake up the app on ios. this is problematic since data need to be stored locally and remotely
            // in something of a reliable schedule; otherwise, we risk data loss (e.g., from device restarts, app kills, etc.).
            // so, do the best possible thing and bug the user with a notification indicating that data need to be stored.
            // only do this for the remote data store to that we don't get duplicate notifications.
            userNotificationMessage = "Please open this notification to submit your data for the \"" + Protocol.Name + "\" study.";
#endif

            _writeCallback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) => WriteAsync(cancellationToken), GetType().FullName, Protocol.Id, Protocol.Id, TimeSpan.FromMinutes(_writeTimeoutMinutes), userNotificationMessage);
            SensusContext.Current.CallbackScheduler.ScheduleRepeatingCallback(_writeCallback, TimeSpan.FromMilliseconds(_writeDelayMS), TimeSpan.FromMilliseconds(_writeDelayMS), WRITE_CALLBACK_LAG);
        }

        public override void Stop()
        {
            SensusContext.Current.CallbackScheduler.UnscheduleCallback(_writeCallback?.Id);
        }

        public override void Reset()
        {
            base.Reset();

            _mostRecentSuccessfulWriteTime = null;
            _writeCallback = null;
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (_mostRecentSuccessfulWriteTime.HasValue)
            {
                TimeSpan timeElapsedSincePreviousWrite = DateTime.Now - _mostRecentSuccessfulWriteTime.Value;
                if (timeElapsedSincePreviousWrite.TotalMilliseconds > (_writeDelayMS + 5000))  // system timer callbacks aren't always fired exactly as scheduled, resulting in health tests that identify warning conditions for delayed data storage. allow a small fudge factor to ignore most of these warnings.
                {
                    warning += "Data store \"" + GetType().FullName + "\" has not written data in " + timeElapsedSincePreviousWrite + " (write delay = " + _writeDelayMS + "ms)." + Environment.NewLine;
                }
            }

            return restart;
        }

        public Task WriteAsync(CancellationToken cancellationToken)
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
            else
            {
                write = true;
            }

            if (write)
            {
                return Task.Run(async () =>
                {
#if __IOS__
                    // on ios the user must activate the app in order to save data. give the user some feedback to let them know that this is 
                    // going to happen and might take some time. if they background the app the write will be canceled if it runs out of background
                    // time.
                    SensusServiceHelper.Get().FlashNotificationAsync("Submitting data. Please wait for success confirmation...");
#endif

                    // instruct the local data store to write its data to the remote data store.
                    await Protocol.LocalDataStore.WriteToRemoteAsync(cancellationToken);

#if __IOS__
                    // on ios the user must activate the app in order to save data. give the user some feedback to let them know that the data were stored remotely.
                    SensusServiceHelper.Get().FlashNotificationAsync("Submitted data to the \"" + Protocol.Name + "\" study. Thank you!");
#endif
                });
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public abstract Task WriteAsync(Stream stream, string name, string contentType, CancellationToken cancellationToken);

        public abstract Task WriteAsync(Datum datum, CancellationToken cancellationToken);

        public abstract string GetDatumKey(Datum datum);

        public abstract Task<T> GetDatum<T>(string datumKey, CancellationToken cancellationToken) where T : Datum;
    }
}