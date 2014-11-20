using Sensus.Exceptions;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    public abstract class DataStore : INotifyPropertyChanged
    {
        /// <summary>
        /// Fired when a UI-relevant property is changed.
        /// </summary>
        [Serializable]
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private int _commitDelayMS;
        [NonSerialized]
        private AutoResetEvent _commitTrigger;
        [NonSerialized]
        private Task _commitTask;
        private bool _running;
        [NonSerialized]
        private Protocol _protocol;

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        [StringUiProperty("Name:", true)]
        public string Name
        {
            get { return _name; }
            set
            {
                if (!value.Equals(_name, StringComparison.Ordinal))
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Commit Delay (MS):", true)]
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

        public bool Running
        {
            get { return _running; }
        }

        public abstract bool NeedsToBeRunning { get; }

        protected abstract string DisplayName { get; }

        public DataStore()
        {
            _name = DisplayName;
            _commitDelayMS = 10000;
            _running = false;
        }

        public virtual void Start()
        {
            if (_running)
                throw new DataStoreException("Datastore already running.");

            if (App.LoggingLevel >= LoggingLevel.Normal)
                App.Get().SensusService.Log("Starting " + GetType().Name + " data store:  " + Name);

            _commitTrigger = new AutoResetEvent(false);  // delay the first commit
            _running = true;
            _commitTask = Task.Run(() =>
                {                  
                    while (NeedsToBeRunning)
                    {
                        if (App.LoggingLevel >= LoggingLevel.Debug)
                            App.Get().SensusService.Log(Name + " is about to wait for " + _commitDelayMS + " MS before committing data.");

                        _commitTrigger.WaitOne(_commitDelayMS);

                        if (App.LoggingLevel >= LoggingLevel.Debug)
                            App.Get().SensusService.Log(Name + " is waking up to commit data.");

                        DataCommitted(CommitData(GetDataToCommit()));  // regardless of whether the commit is triggered by the delay or by Stop, we should commit existing data.
                    }

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Exited while-loop for data store " + Name);

                    _running = false;
                });
        }

        protected abstract ICollection<Datum> GetDataToCommit();

        protected abstract ICollection<Datum> CommitData(ICollection<Datum> data);

        protected abstract void DataCommitted(ICollection<Datum> data);

        public Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    // data stores will automatically stop if NeedsToBeRunning becomes false. however, we might be in the middle of a very long commit delay, in which case the Stop method serves a purpose by immediately triggering a commit and stopping the thread
                    if (!_running)
                        return;

                    if (NeedsToBeRunning)
                        throw new DataStoreException("DataStore " + Name + " cannot be stopped while it is needed.");

                    _running = false;

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Setting data store " + Name + "'s wait handle within Stop method.");

                    _commitTrigger.Set();

                    await _commitTask;

                    if (App.LoggingLevel >= LoggingLevel.Normal)
                        App.Get().SensusService.Log("Data store " + Name + "'s task ended.");
                });
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
