using Newtonsoft.Json;
using SensusService.Exceptions;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SensusService.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    public abstract class DataStore : INotifyPropertyChanged
    {
        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private int _commitDelayMS;
        private AutoResetEvent _commitTrigger;
        private Task _commitTask;
        private bool _running;
        private Protocol _protocol;
        private DateTimeOffset _mostRecentCommitTimestamp;
        private List<Datum> _nonProbeDataToCommit;

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Commit Delay (MS):", true, 2)]
        public int CommitDelayMS
        {
            get { return _commitDelayMS; }
            set
            {
                if (value != _commitDelayMS)
                {
                    _commitDelayMS = value;
                    OnPropertyChanged();
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

        protected abstract string DisplayName { get; }

        [JsonIgnore]
        public abstract bool Clearable { get; }

        [DisplayStringUiProperty("Last:", int.MaxValue)]
        public DateTimeOffset MostRecentCommitTimestamp
        {
            get { return _mostRecentCommitTimestamp; }
            set
            {
                if (value != _mostRecentCommitTimestamp)
                {
                    _mostRecentCommitTimestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public DataStore()
        {
            _name = DisplayName;
            _commitDelayMS = 10000;
            _running = false;
            _mostRecentCommitTimestamp = DateTimeOffset.MinValue;
            _nonProbeDataToCommit = new List<Datum>();
        }

        public void AddNonProbeDatum(Datum datum)
        {
            lock (_nonProbeDataToCommit)
                _nonProbeDataToCommit.Add(datum);
        }

        public virtual void Start()
        {
            lock (this)
            {
                if (_running)
                    return;
                else
                    _running = true;

                SensusServiceHelper.Get().Logger.Log("Starting " + GetType().Name + " data store:  " + Name, LoggingLevel.Normal);

                _commitTrigger = new AutoResetEvent(false);  // delay the first commit  

                _commitTask = Task.Run(() =>
                    {
                        while (_running)
                        {
                            SensusServiceHelper.Get().Logger.Log(Name + " is about to wait for " + _commitDelayMS + " MS before committing data.", LoggingLevel.Verbose);

                            _commitTrigger.WaitOne(_commitDelayMS);

                            if (_running)
                            {
                                SensusServiceHelper.Get().Logger.Log(Name + " is waking up to commit data.", LoggingLevel.Verbose);

                                ICollection<Datum> dataToCommit = null;
                                try
                                {
                                    dataToCommit = GetDataToCommit();
                                    if (dataToCommit == null)
                                        throw new DataStoreException("Null collection returned by GetDataToCommit");
                                }
                                catch (Exception ex) { SensusServiceHelper.Get().Logger.Log(Name + " failed to get data to commit:  " + ex.Message, LoggingLevel.Normal); }

                                if (dataToCommit != null)
                                {
                                    lock (_nonProbeDataToCommit)
                                        foreach (Datum datum in _nonProbeDataToCommit)
                                            dataToCommit.Add(datum);

                                    ICollection<Datum> committedData = null;
                                    try
                                    {
                                        committedData = CommitData(dataToCommit);
                                        if (committedData == null)
                                            throw new DataStoreException("Null collection returned by CommitData");
                                    }
                                    catch (Exception ex) { SensusServiceHelper.Get().Logger.Log(Name + " failed to commit data:  " + ex.Message, LoggingLevel.Normal); }

                                    if (committedData != null)
                                        try
                                        {
                                            // remove any non-probe data that were committed from the in-memory store
                                            lock (_nonProbeDataToCommit)
                                                foreach (Datum datum in committedData)
                                                    _nonProbeDataToCommit.Remove(datum);

                                            ProcessCommittedData(committedData);
                                            MostRecentCommitTimestamp = DateTimeOffset.UtcNow;
                                        }
                                        catch (Exception ex) { SensusServiceHelper.Get().Logger.Log(Name + " failed to process committed data:  " + ex.Message, LoggingLevel.Normal); }
                                }
                            }
                        }

                        SensusServiceHelper.Get().Logger.Log("Exited while-loop for data store " + Name, LoggingLevel.Normal);
                    });
            }
        }

        protected abstract ICollection<Datum> GetDataToCommit();

        protected abstract ICollection<Datum> CommitData(ICollection<Datum> data);

        protected abstract void ProcessCommittedData(ICollection<Datum> committedData);

        public virtual void Clear() { }

        public virtual void Stop()
        {
            lock (this)
            {
                if (_running)
                    _running = false;
                else
                    return;

                SensusServiceHelper.Get().Logger.Log("Stopping " + GetType().Name + " data store:  " + Name, LoggingLevel.Normal);

                if (_commitTrigger != null)
                    _commitTrigger.Set();

                if (_commitTask != null)
                    _commitTask.Wait();
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

        public virtual bool Ping(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            if (!_running)
            {
                error += "Datastore \"" + _name + "\" is not running." + Environment.NewLine;
                restart = true;
            }

            double msElapsedSinceLastCommit = (DateTime.UtcNow - _mostRecentCommitTimestamp).TotalMilliseconds;
            if (msElapsedSinceLastCommit > _commitDelayMS)
                warning += "Datastore \"" + _name + "\" has not committed data in " + msElapsedSinceLastCommit + "ms (commit delay = " + _commitDelayMS + "ms)." + Environment.NewLine;

            return restart;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
