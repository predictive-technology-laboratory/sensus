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

using SensusService.Probes;
using SensusUI.UiProperties;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace SensusService.DataStores.Local
{
    /// <summary>
    /// Responsible for transferring data from probes to local media.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        private bool _uploadToRemoteDataStore;

        [OnOffUiProperty("Upload to Remote:", true, 3)]
        public bool UploadToRemoteDataStore
        {
            get { return _uploadToRemoteDataStore; }
            set { _uploadToRemoteDataStore = value; }
        }

        [JsonIgnore]
        public abstract int DataCount { get; }

        protected LocalDataStore()
        {
            _uploadToRemoteDataStore = true;

            #if DEBUG
            CommitDelayMS = 5000;  // 5 seconds...so we can see debugging output quickly
            #else
            CommitDelayMS = 60000;
            #endif
        }

        protected sealed override List<Datum> GetDataToCommit()
        {
            List<Datum> dataToCommit = new List<Datum>();
            foreach (Probe probe in Protocol.Probes)
            {
                // the collected data object comes directly from the probe, so lock it down before working with it.
                ICollection<Datum> collectedData = probe.GetCollectedData();
                if (collectedData != null)
                    lock (collectedData)
                        if (collectedData.Count > 0)
                            dataToCommit.AddRange(collectedData);
            }

            SensusServiceHelper.Get().Logger.Log("Retrieved " + dataToCommit.Count + " data elements from probes.", LoggingLevel.Debug, GetType());

            return dataToCommit;
        }

        protected sealed override void ProcessCommittedData(List<Datum> committedData)
        {
            SensusServiceHelper.Get().Logger.Log("Clearing " + committedData.Count + " committed data elements from probes.", LoggingLevel.Debug, GetType());

            foreach (Probe probe in Protocol.Probes)
                probe.ClearDataCommittedToLocalDataStore(committedData);

            SensusServiceHelper.Get().Logger.Log("Done clearing committed data elements from probes.", LoggingLevel.Verbose, GetType());
        }

        public List<Datum> GetDataForRemoteDataStore() 
        {
            return GetDataForRemoteDataStore(null, null);
        }

        public abstract List<Datum> GetDataForRemoteDataStore(Action<double> progressCallback, Func<bool> cancelCallback);

        public abstract void ClearDataCommittedToRemoteDataStore(List<Datum> dataCommittedToRemote);
    }
}
