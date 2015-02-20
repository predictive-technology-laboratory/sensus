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
using System.Collections.Generic;

namespace SensusService.DataStores.Remote
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

        public RemoteDataStore()
        {
            _requireWiFi = true;
            _requireCharging = true;

            #if DEBUG
            CommitDelayMS = 10000;  // 10 seconds...so we can see debugging output quickly
            #else
            CommitDelayMS = 1000 * 60 * 30;  // every thirty minutes
            #endif
        }

        protected sealed override List<Datum> GetDataToCommit()
        {
            List<Datum> dataToCommit = new List<Datum>();

            if (!Protocol.LocalDataStore.UploadToRemoteDataStore)
                SensusServiceHelper.Get().Logger.Log("Not committing local data to remote data store.", LoggingLevel.Verbose, GetType());
            else if (_requireWiFi && !SensusServiceHelper.Get().WiFiConnected)
                SensusServiceHelper.Get().Logger.Log("Required WiFi but device WiFi is not connected.", LoggingLevel.Verbose, GetType());
            else if (_requireCharging && !SensusServiceHelper.Get().IsCharging)
                SensusServiceHelper.Get().Logger.Log("Required charging but device is not charging.", LoggingLevel.Verbose, GetType());
            else
            {
                dataToCommit = Protocol.LocalDataStore.GetDataForRemoteDataStore();
                SensusServiceHelper.Get().Logger.Log("Retrieved " + dataToCommit.Count + " data elements from local data store.", LoggingLevel.Debug, GetType());
            }

            return dataToCommit;
        }

        protected sealed override void ProcessCommittedData(List<Datum> committedData)
        {
            Protocol.LocalDataStore.ClearDataCommittedToRemoteDataStore(committedData);
        }
    }
}
