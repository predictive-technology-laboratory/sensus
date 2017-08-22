﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

namespace Sensus.DataStores.Remote
{
    /// <summary>
    /// Responsible for transferring data from a local data store to remote media.
    /// </summary>
    public abstract class RemoteDataStore : DataStore
    {
        private bool _requireWiFi;
        private bool _requireCharging;

        [OnOffUiProperty("Require WiFi:", true, int.MaxValue)]
        public bool RequireWiFi
        {
            get { return _requireWiFi; }
            set { _requireWiFi = value; }
        }

        [OnOffUiProperty("Require Charging:", true, int.MaxValue)]
        public bool RequireCharging
        {
            get { return _requireCharging; }
            set { _requireCharging = value; }
        }

        [JsonIgnore]
        public abstract bool CanRetrieveCommittedData { get; }

        protected RemoteDataStore()
        {
            _requireWiFi = true;
            _requireCharging = true;

#if DEBUG || ENABLE_TEST_CLOUD
            CommitDelayMS = 10000;  // 10 seconds...so we can see debugging output quickly
#else
            CommitDelayMS = 1000 * 60 * 60;  // every 60 minutes
#endif
        }

        public sealed override Task CommitAndReleaseAddedDataAsync(CancellationToken cancellationToken)
        {
            bool commit = false;

            if (cancellationToken.IsCancellationRequested)
            {
                SensusServiceHelper.Get().Logger.Log("Cancelled commit to remote data store via token.", LoggingLevel.Normal, GetType());
            }
            else if (!Protocol.LocalDataStore.UploadToRemoteDataStore)
            {
                SensusServiceHelper.Get().Logger.Log("Remote data store upload is disabled.", LoggingLevel.Normal, GetType());
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
                commit = true;
            }

            if (commit)
            {
                return Task.Run(async () =>
                {
#if __IOS__
                    // on ios the user must activate the app in order to save data. give the user some feedback to let them know that this is 
                    // going to happen and might take some time. if they background the app the commit will be canceled if it runs out of background
                    // time.
                    SensusServiceHelper.Get().FlashNotificationAsync("Submitting data. Please wait for success confirmation...");
#endif

                    // first commmit any data that have accumulated in the internal memory of the remote data store, e.g., protocol report
                    // data when we are not committing local data to remote but we are also forcing report data.
                    await base.CommitAndReleaseAddedDataAsync(cancellationToken);

                    // instruct the local data store to commit its data to the remote data store.
                    Protocol.LocalDataStore.CommitToRemote(cancellationToken);

#if __IOS__
                    // on ios the user must activate the app in order to save data. give the user some feedback to let them know that the data were stored remotely.
                    SensusServiceHelper.Get().FlashNotificationAsync("Submitted data to the \"" + Protocol.Name + "\" study. Thank you!");
#endif
                });
            }
            else
                return Task.FromResult(false);
        }

        public abstract string GetDatumKey(Datum datum);

        public abstract Task<T> GetDatum<T>(string datumKey, CancellationToken cancellationToken) where T : Datum;
    }
}