#region copyright
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
#endregion

using Newtonsoft.Json;
using SensusService.Exceptions;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SensusService.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    public abstract class DataStore
    {
        private string _name;
        private int _commitDelayMS;
        private Thread _commitThread;
        private bool _running;
        private Protocol _protocol;
        private DateTimeOffset _mostRecentCommitTimestamp;
        private List<Datum> _nonProbeDataToCommit;
        private bool _isCommitting;

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [EntryIntegerUiProperty("Commit Delay (MS):", true, 2)]
        public int CommitDelayMS
        {
            get { return _commitDelayMS; }
            set { _commitDelayMS = value; }
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

        protected abstract string DisplayName { get; }

        [JsonIgnore]
        public abstract bool Clearable { get; }

        public DataStore()
        {
            _name = DisplayName;
            _commitDelayMS = 10000;
            _running = false;
            _mostRecentCommitTimestamp = DateTimeOffset.MinValue;
            _nonProbeDataToCommit = new List<Datum>();
            _isCommitting = false;                   
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
            lock (this)
            {
                if (_running)
                    return;
                else
                    _running = true;

                SensusServiceHelper.Get().Logger.Log("Starting " + GetType().Name + " data store:  " + Name, LoggingLevel.Normal);

                _commitThread = new Thread(() =>
                    {
                        int msToSleep = _commitDelayMS;

                        while (_running)
                        {
                            // in order to allow the commit thread to be interrupted by Stop, sleep for 1-second intervals.
                            SensusServiceHelper.Get().Logger.Log(GetType().Name + " is sleeping.", LoggingLevel.Debug);
                            Thread.Sleep(1000);
                            msToSleep -= 1000;

                            // have we slept enough to run a commit? if not, continue the loop.
                            if (msToSleep > 0)
                                continue;

                            // if we're still running, execute a commit.
                            if (_running)
                            {
                                _isCommitting = true;

                                SensusServiceHelper.Get().Logger.Log(_name + " is committing data.", LoggingLevel.Verbose);

                                List<Datum> dataToCommit = null;
                                try
                                {
                                    dataToCommit = GetDataToCommit();
                                    if (dataToCommit == null)
                                        throw new DataStoreException("Null collection returned by GetDataToCommit");
                                }
                                catch (Exception ex) { SensusServiceHelper.Get().Logger.Log(_name + " failed to get data to commit:  " + ex.Message, LoggingLevel.Normal); }

                                if (dataToCommit != null)
                                {
                                    // add in non-probe data (e.g., that from protocol reports)
                                    lock (_nonProbeDataToCommit)
                                        foreach (Datum datum in _nonProbeDataToCommit)
                                            dataToCommit.Add(datum);

                                    List<Datum> committedData = null;
                                    try
                                    {
                                        committedData = CommitData(dataToCommit);                                                                             

                                        if (committedData == null)
                                            throw new DataStoreException("Null collection returned by CommitData");

                                        _mostRecentCommitTimestamp = DateTimeOffset.UtcNow;
                                    }
                                    catch (Exception ex) { SensusServiceHelper.Get().Logger.Log(_name + " failed to commit data:  " + ex.Message, LoggingLevel.Normal); }

                                    if (committedData != null && committedData.Count > 0)
                                        try
                                        {
                                            // remove any non-probe data that were committed from the in-memory store
                                            lock (_nonProbeDataToCommit)
                                                foreach (Datum datum in committedData)
                                                    _nonProbeDataToCommit.Remove(datum);

                                            ProcessCommittedData(committedData);
                                        }
                                        catch (Exception ex) { SensusServiceHelper.Get().Logger.Log(_name + " failed to process committed data:  " + ex.Message, LoggingLevel.Normal); }
                                }

                                _isCommitting = false;
                            }

                            msToSleep = _commitDelayMS;
                        }

                        SensusServiceHelper.Get().Logger.Log("Exited while-loop for data store " + _name, LoggingLevel.Normal);
                    });

                _commitThread.Start();
            }
        }

        protected abstract List<Datum> GetDataToCommit();

        protected abstract List<Datum> CommitData(List<Datum> data);

        protected abstract void ProcessCommittedData(List<Datum> committedData);

        public virtual void Clear() { }

        /// <summary>
        /// Stops the commit thread. This should always be called first within parent-class overrides.
        /// </summary>
        public virtual void Stop()
        {
            lock (this)
            {
                if (_running)
                    _running = false;
                else
                    return;

                SensusServiceHelper.Get().Logger.Log("Stopping " + GetType().Name + " data store:  " + Name, LoggingLevel.Normal);

                // since _running is now false, the commit thread will be exiting soon. if it's in the middle of a commit, the commit will finish.
                _commitThread.Join();
            }
        }

        public void Restart()
        {
            lock (this)
            {
                Stop();
                Start();
            }
        }

        public virtual bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            if (!_running)
            {
                error += "Datastore \"" + _name + "\" is not running." + Environment.NewLine;
                restart = true;
            }

            double msElapsedSinceLastCommit = (DateTimeOffset.UtcNow - _mostRecentCommitTimestamp).TotalMilliseconds;
            if (!_isCommitting && msElapsedSinceLastCommit > _commitDelayMS)
                warning += "Datastore \"" + _name + "\" has not committed data in " + msElapsedSinceLastCommit + "ms (commit delay = " + _commitDelayMS + "ms)." + Environment.NewLine;

            return restart;
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
