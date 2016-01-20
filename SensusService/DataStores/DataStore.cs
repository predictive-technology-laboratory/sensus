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

using Newtonsoft.Json;
using SensusService.Exceptions;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace SensusService.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    public abstract class DataStore
    {
        private int _commitDelayMS;
        private bool _running;
        private Protocol _protocol;
        private DateTime? _mostRecentSuccessfulCommitTime;
        private List<Datum> _nonProbeDataToCommit;
        private string _commitCallbackId;

        [EntryIntegerUiProperty("Commit Delay (MS):", true, 2)]
        public int CommitDelayMS
        {
            get { return _commitDelayMS; }
            set
            {
                if (value <= 1000)
                    value = 1000;
                
                if (value != _commitDelayMS)
                {
                    _commitDelayMS = value; 

                    if (_commitCallbackId != null)
                        _commitCallbackId = SensusServiceHelper.Get().RescheduleRepeatingCallback(_commitCallbackId, _commitDelayMS, _commitDelayMS);
                }
            }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
        }

        [JsonIgnore]
        public abstract string DisplayName { get; }

        [JsonIgnore]
        public abstract bool Clearable { get; }

        protected DataStore()
        {
            _commitDelayMS = 10000;
            _running = false;
            _mostRecentSuccessfulCommitTime = null;
            _nonProbeDataToCommit = new List<Datum>();
            _commitCallbackId = null;
        }

        public void AddNonProbeDatum(Datum datum)
        {
            lock (_nonProbeDataToCommit)
                _nonProbeDataToCommit.Add(datum);
        }

        /// <summary>
        /// Starts the commit thread. This should always be called last within child-class overrides.
        /// </summary>
        public virtual void Start()
        {
            if (!_running)
            {
                _running = true;
                SensusServiceHelper.Get().Logger.Log("Starting.", LoggingLevel.Normal, GetType());
                _mostRecentSuccessfulCommitTime = DateTime.Now;

                // use the async version of commit so that we don't hang for unreliable commit operations (e.g., AWS S3 commits). this means that all commit 
                // operations for a datastore must be suitable for running concurrently. ultimately, this might mean that duplicate data are committed either
                // locally or remotely, but we can live with that since the alternative involves the potential for infinite hangs.
                _commitCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(CommitAsync, GetType().FullName + " Commit", _commitDelayMS, _commitDelayMS);
            }
        }

        public void CommitAsync(CancellationToken cancellationToken, bool flashOnError, Action callback)
        {
            new Thread(() =>
                {
                    try
                    {
                        Commit(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        if (flashOnError)
                            SensusServiceHelper.Get().FlashNotificationAsync("Failed to commit data:  " + ex.Message);
                    }

                    if (callback != null)
                        callback();
                    
                }).Start();
        }

        private void CommitAsync(string callbackId, CancellationToken cancellationToken)
        {
            new Thread(() =>
                {
                    Commit(cancellationToken);

                }).Start();
        }

        private void Commit(CancellationToken cancellationToken)
        {
            if (_running)
            {
                try
                {
                    SensusServiceHelper.Get().Logger.Log("Committing data.", LoggingLevel.Normal, GetType());

                    DateTime commitStartTime = DateTime.Now;

                    List<Datum> dataToCommit = null;
                    try
                    {
                        dataToCommit = GetDataToCommit(cancellationToken);
                        if (dataToCommit == null)
                            throw new DataStoreException("Null collection returned by GetDataToCommit");
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to get data to commit:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }

                    if (dataToCommit != null && !cancellationToken.IsCancellationRequested)
                    {
                        // add in non-probe data (e.g., that from protocol reports or participation reward verifications)
                        lock (_nonProbeDataToCommit)
                            foreach (Datum datum in _nonProbeDataToCommit)
                                dataToCommit.Add(datum);

                        List<Datum> committedData = null;
                        try
                        {
                            committedData = CommitData(dataToCommit, cancellationToken);                                                                             

                            if (committedData == null)
                                throw new DataStoreException("Null collection returned by CommitData");

                            _mostRecentSuccessfulCommitTime = DateTime.Now;
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to commit data:  " + ex.Message, LoggingLevel.Normal, GetType());
                        }

                        // don't check cancellation token here, since we've committed data and need to process the results (i.e., remove from probe caches or delete from local data store). if we don't always do this we'll end up committing duplicate data on next commit.
                        if (committedData != null && committedData.Count > 0)
                        {
                            try
                            {
                                // remove any non-probe data that were committed from the in-memory store
                                lock (_nonProbeDataToCommit)
                                    foreach (Datum datum in committedData)
                                        _nonProbeDataToCommit.Remove(datum);

                                ProcessCommittedData(committedData);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to process committed data:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                        }
                    }

                    SensusServiceHelper.Get().Logger.Log("Finished commit in " + (DateTime.Now - commitStartTime).TotalSeconds + " seconds.", LoggingLevel.Normal, GetType());
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to run Commit:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
        }

        protected abstract List<Datum> GetDataToCommit(CancellationToken cancellationToken);

        protected abstract List<Datum> CommitData(List<Datum> data, CancellationToken cancellationToken);

        protected abstract void ProcessCommittedData(List<Datum> committedData);

        public virtual void Clear()
        {
        }

        public bool HasNonProbeDatumToCommit(string datumId)
        {
            return _nonProbeDataToCommit.Any(datum => datum.Id == datumId);
        }

        /// <summary>
        /// Stops the commit thread. This should always be called first within parent-class overrides.
        /// </summary>
        public virtual void Stop()
        {
            if (_running)
            {
                _running = false;
                SensusServiceHelper.Get().Logger.Log("Stopping.", LoggingLevel.Normal, GetType());
                SensusServiceHelper.Get().UnscheduleRepeatingCallback(_commitCallbackId);
                _commitCallbackId = null;
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public virtual bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            if (!_running)
            {
                error += "Datastore \"" + GetType().FullName + "\" is not running." + Environment.NewLine;
                restart = true;
            }

            double msElapsedSinceLastCommit = (DateTime.Now - _mostRecentSuccessfulCommitTime.GetValueOrDefault()).TotalMilliseconds;
            if (msElapsedSinceLastCommit > (_commitDelayMS + 5000))  // system timer callbacks aren't always fired exactly as scheduled, resulting in health tests that identify warning conditions for delayed data storage. allow a small fudge factor to ignore most of these warnings warnings.
                warning += "Datastore \"" + GetType().FullName + "\" has not committed data in " + msElapsedSinceLastCommit + "ms (commit delay = " + _commitDelayMS + "ms)." + Environment.NewLine;

            return restart;
        }

        public virtual void ClearForSharing()
        {
            if (_running)
                throw new Exception("Cannot clear data store for sharing while it is running.");
            
            _mostRecentSuccessfulCommitTime = null;
            _nonProbeDataToCommit.Clear();
            _commitCallbackId = null;
        }

        public DataStore Copy()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };

            return JsonConvert.DeserializeObject<DataStore>(JsonConvert.SerializeObject(this, settings), settings);
        }
    }
}