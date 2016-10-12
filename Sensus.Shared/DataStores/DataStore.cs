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
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sensus.Shared.UI.UiProperties;
using Sensus.Shared.Exceptions;
using Sensus.Shared.DataStores.Remote;

namespace Sensus.Shared.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    public abstract class DataStore
    {
        private const int COMMIT_CHUNK_SIZE = 10000;

        protected static Task CommitChunksAsync(HashSet<Datum> data, DataStore dataStore, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                // the data set passed in is open to modification, and callers might continue adding data after this method returns.
                // so, we can't simply run until data is empty since it may never be empty. instead, run a predetermined number of 
                // chunks.
                int maxNumChunks;
                lock (data)
                {
                    maxNumChunks = (int)Math.Ceiling(data.Count / (double)COMMIT_CHUNK_SIZE);
                }

                HashSet<Datum> chunk = new HashSet<Datum>();

                // process all chunks, stopping for cancellation.
                int chunksCommitted = 0;
                while (!cancellationToken.IsCancellationRequested && chunksCommitted < maxNumChunks)
                {
                    // build a new chunk, stopping for cancellation, exhaustion of data, or full chunk.
                    chunk.Clear();

                    lock (data)
                    {
                        foreach (Datum datum in data)
                        {
                            chunk.Add(datum);

                            if (cancellationToken.IsCancellationRequested || chunk.Count >= COMMIT_CHUNK_SIZE)
                                break;
                        }
                    }

                    // commit chunk as long as we're not canceled and the chunk has something in it
                    int dataCommitted = 0;
                    if (!cancellationToken.IsCancellationRequested && chunk.Count > 0)
                    {
                        List<Datum> committedData = await dataStore.CommitAsync(chunk, cancellationToken);

                        lock (data)
                        {
                            foreach (Datum committedDatum in committedData)
                            {
                                // remove committed data from the data that were passed in. if we check for and break
                                // on cancellation here, the committed data will not be treated as such. we need to 
                                // remove them from the data collection to indicate to the caller that they were committed.
                                data.Remove(committedDatum);
                                ++dataCommitted;
                                ++dataStore.CommittedDataCount;
                            }
                        }
                    }

                    // if we failed to commit anything, then we've been canceled, there's nothing to commit, or the commit failed.
                    // in any of these cases, we should not proceed with the next chunk. the caller will need to retry the commit.
                    if (dataCommitted == 0)
                        break;

                    ++chunksCommitted;
                }
            });
        }

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
        private HashSet<Datum> _data;
        private string _commitCallbackId;
        private long _addedDataCount;
        private long _committedDataCount;
        private bool _sizeTriggeredCommitRunning;
        private bool _forcedCommitRunning;

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
        protected DateTime? MostRecentSuccessfulCommitTime
        {
            get
            {
                return _mostRecentSuccessfulCommitTime;
            }
            set
            {
                _mostRecentSuccessfulCommitTime = value;
            }
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

        /// <summary>
        /// Gets or sets the number of data ever commited to this data store.
        /// </summary>
        /// <value>The committed data count.</value>
        public long CommittedDataCount
        {
            get
            {
                return _committedDataCount;
            }

            set
            {
                _committedDataCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of data ever added to this data store.
        /// </summary>
        /// <value>The added data count.</value>
        public long AddedDataCount
        {
            get
            {
                return _addedDataCount;
            }

            set
            {
                _addedDataCount = value;
            }
        }

        protected DataStore()
        {
            _commitDelayMS = 10000;
            _commitTimeoutMinutes = 5;
            _running = false;
            _mostRecentSuccessfulCommitTime = null;
            _data = new HashSet<Datum>();
            _commitCallbackId = null;
            _committedDataCount = 0;
            _addedDataCount = 0;
            _sizeTriggeredCommitRunning = false;
            _forcedCommitRunning = false;
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

#if __IOS__
                // we can't wake up the app on ios. this is problematic since data need to be stored locally and remotely
                // in something of a reliable schedule; otherwise, we risk data loss (e.g., from device restarts, app kills, etc.).
                // so, do the best possible thing and bug the user with a notification indicating that data need to be stored.
                // only do this for the remote data store to that we don't get duplicate notifications.
                if (this is RemoteDataStore)
                    userNotificationMessage = "Sensus needs to submit your data for the \"" + _protocol.Name + "\" study. Please open this notification.";
#endif

                ScheduledCallback callback = new ScheduledCallback(GetType().FullName + " Commit", CycleAsync, TimeSpan.FromMinutes(_commitTimeoutMinutes), userNotificationMessage);
                _commitCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(callback, _commitDelayMS, _commitDelayMS, COMMIT_CALLBACK_LAG);
            }
        }

        public Task AddAsync(Datum datum, CancellationToken cancellationToken, bool forceCommit)
        {
            lock (_data)
            {
                _data.Add(datum);
                ++_addedDataCount;

                SensusServiceHelper.Get().Logger.Log("Stored datum:  " + datum.GetType().Name, LoggingLevel.Debug, GetType());

                if (!_sizeTriggeredCommitRunning && !_forcedCommitRunning)
                {
                    // if we've accumulated a chunk, commit it locally to reduce memory pressure
                    if (_data.Count >= COMMIT_CHUNK_SIZE)
                    {
                        SensusServiceHelper.Get().Logger.Log("Running size-triggered commit.", LoggingLevel.Normal, GetType());

                        _sizeTriggeredCommitRunning = true;

                        return Task.Run(async () =>
                        {
                            try
                            {
                                await CommitChunksAsync(_data, this, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to run size-triggered commit:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                            finally
                            {
                                _sizeTriggeredCommitRunning = false;
                            }
                        });
                    }
                    else if (forceCommit)
                    {
                        SensusServiceHelper.Get().Logger.Log("Running forced commit.", LoggingLevel.Normal, GetType());

                        _forcedCommitRunning = true;

                        return Task.Run(async () =>
                        {
                            try
                            {
                                await CommitChunksAsync(_data, this, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to run forced commit:  " + ex.Message, LoggingLevel.Normal, GetType());
                            }
                            finally
                            {
                                _forcedCommitRunning = false;
                            }
                        });
                    }
                }

                return Task.FromResult(false);
            }
        }

        protected virtual Task CycleAsync(string callbackId, CancellationToken cancellationToken, Action letDeviceSleepCallback)
        {
            return Task.Run(async () =>
            {
                if (_running)
                    await CommitChunksAsync(_data, this, cancellationToken);
            });
        }

        public async Task<bool> CommitAsync(Datum datum, CancellationToken cancellationToken)
        {
            return (await CommitAsync(new Datum[] { datum }, cancellationToken)).Contains(datum);
        }

        public abstract Task<List<Datum>> CommitAsync(IEnumerable<Datum> data, CancellationToken cancellationToken);

        public virtual void Clear()
        {
            lock (_data)
            {
                _data.Clear();
            }
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

            lock (_data)
            {
                string dataStoreName = GetType().Name;
                misc += dataStoreName + " - added:  " + _addedDataCount + Environment.NewLine +
                        dataStoreName + " - in memory:  " + _data.Count + Environment.NewLine +
                        dataStoreName + " - committed:  " + _committedDataCount + Environment.NewLine;
            }

            return restart;
        }

        public virtual void Reset()
        {
            if (_running)
                throw new Exception("Cannot reset data store while it is running.");

            _mostRecentSuccessfulCommitTime = null;
            _commitCallbackId = null;
            _addedDataCount = 0;
            _committedDataCount = 0;

            lock (_data)
            {
                _data.Clear();
            }
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

            try
            {
                SensusServiceHelper.Get().FlashNotificationsEnabled = false;
                return JsonConvert.DeserializeObject<DataStore>(JsonConvert.SerializeObject(this, settings), settings);
            }
            catch (Exception ex)
            {
                string message = $"Failed to copy data store:  {ex.Message}";
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                SensusException.Report(message, ex);
                return null;
            }
            finally
            {
                SensusServiceHelper.Get().FlashNotificationsEnabled = true;
            }
        }
    }
}