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
using SensusService.DataStores.Remote;
using System.Threading.Tasks;

namespace SensusService.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    public abstract class DataStore
    {
        /// <summary>
        /// We don't mind commit callbacks lag, since it don't affect any performance metrics and
        /// the latencies aren't inspected when testing data store health or participation. It also
        /// doesn't make sense to force rapid commits since data will not have accumulated.
        /// </summary>
        private const bool COMMIT_CALLBACK_LAG = true;

        private int _commitDelayMS;
        private int _commitTimeoutMinutes;
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
                        _commitCallbackId = SensusServiceHelper.Get().RescheduleRepeatingCallback(_commitCallbackId, _commitDelayMS, _commitDelayMS, COMMIT_CALLBACK_LAG);
                }
            }
        }

        [EntryIntegerUiProperty("Commit Timeout (Mins.):", true, 3)]
        public int CommitTimeoutMinutes
        {
            get
            {
                return _commitTimeoutMinutes;
            }
            set
            {
                if (value <= 0)
                    value = 1;

                _commitTimeoutMinutes = value;
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
            _commitTimeoutMinutes = 5;
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
                string userNotificationMessage = null;

                // we can't wake up the app on ios. this is problematic since data need to be stored locally and remotely
                // in something of a reliable schedule; otherwise, we risk data loss (e.g., from device restarts, app kills, etc.).
                // so, do the best possible thing and bug the user with a notification indicating that data need to be stored.
                // only do this for the remote data store to that we don't get duplicate notifications.
#if __IOS__
                if (this is RemoteDataStore)
                    userNotificationMessage = "Sensus needs to submit your data for the \"" + _protocol.Name + "\" study. Please open this notification.";
#endif

                ScheduledCallback callback = new ScheduledCallback(CycleAsync, GetType().FullName + " Commit", TimeSpan.FromMinutes(_commitTimeoutMinutes), userNotificationMessage);
                _commitCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(callback, _commitDelayMS, _commitDelayMS, COMMIT_CALLBACK_LAG);
            }
        }

        private Task CycleAsync(string callbackId, CancellationToken cancellationToken, Action letDeviceSleepCallback)
        {
            return Task.Run(async () =>
                {
                    if (_running)
                    {
                        try
                        {

#if __IOS__
                            // on ios the user must activate the app in order to save data. give the user some feedback to let them know that this is 
                            // going to happen and might take some time. if they background the app the commit will be canceled if it runs out of background
                            // time.
                            if (this is RemoteDataStore)
                                SensusServiceHelper.Get().FlashNotificationAsync("Submitting data. Please wait for success confirmation...");
#endif

                            SensusServiceHelper.Get().Logger.Log("Committing data.", LoggingLevel.Normal, GetType());

                            DateTime commitStartTime = DateTime.Now;

                            List<Datum> dataToCommit = null;
                            try
                            {
                                dataToCommit = GetDataToCommit(cancellationToken);
                                if (dataToCommit == null)
                                    throw new SensusException("Null collection returned by GetDataToCommit");
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to get data to commit:  " + ex.Message, LoggingLevel.Normal, GetType(), true);
                            }

                            int? numDataCommitted = null;

                            if (dataToCommit != null && !cancellationToken.IsCancellationRequested)
                            {
                                // add in non-probe data (e.g., that from protocol reports or participation reward verifications)
                                lock (_nonProbeDataToCommit)
                                    foreach (Datum datum in _nonProbeDataToCommit)
                                        dataToCommit.Add(datum);

                                List<Datum> committedData = null;
                                try
                                {
                                    committedData = await CommitDataAsync(dataToCommit, cancellationToken);

                                    if (committedData == null)
                                        throw new SensusException("Null collection returned by CommitData");

                                    _mostRecentSuccessfulCommitTime = DateTime.Now;
                                    numDataCommitted = committedData.Count;
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Failed to commit data:  " + ex.Message, LoggingLevel.Normal, GetType(), true);
                                }

                                // don't check cancellation token here, since we've committed data and need to process the results (i.e., remove from probe caches or delete from local data store). if we don't always do this we'll end up committing duplicate data on next commit.
                                if (committedData != null && committedData.Count > 0)
                                {
                                    try
                                    {
                                        // remove any non-probe data that were committed from the in-memory store.
                                        lock (_nonProbeDataToCommit)
                                            foreach (Datum datum in committedData)
                                                _nonProbeDataToCommit.Remove(datum);

                                        ProcessCommittedData(committedData);
                                    }
                                    catch (Exception ex)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Failed to process committed data:  " + ex.Message, LoggingLevel.Normal, GetType(), true);
                                    }
                                }
                            }

                            SensusServiceHelper.Get().Logger.Log("Finished commit in " + (DateTime.Now - commitStartTime).TotalSeconds + " seconds.", LoggingLevel.Normal, GetType());

#if __IOS__
                            // on ios the user must activate the app in order to save data. give the user some feedback to let them know that the data were stored remotely.
                            if (numDataCommitted != null && this is RemoteDataStore)
                            {
                                int numDataCommittedValue = numDataCommitted.GetValueOrDefault();
                                SensusServiceHelper.Get().FlashNotificationAsync("Submitted " + numDataCommittedValue + " data item" + (numDataCommittedValue == 1 ? "" : "s") + " to the \"" + _protocol.Name + "\" study." + (numDataCommittedValue > 0 ? " Thank you!" : ""));
                            }
#endif
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Commit failed:  " + ex.Message, LoggingLevel.Normal, GetType(), true);
                        }
                    }
                });
        }

        protected abstract List<Datum> GetDataToCommit(CancellationToken cancellationToken);

        public Task<bool> CommitDatumAsync(Datum datum, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                return (await CommitDataAsync(new Datum[] { datum }.ToList(), cancellationToken)).Contains(datum);
            });
        }
                            
        public abstract Task<List<Datum>> CommitDataAsync(List<Datum> data, CancellationToken cancellationToken);

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
                SensusServiceHelper.Get().UnscheduleCallback(_commitCallbackId);
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