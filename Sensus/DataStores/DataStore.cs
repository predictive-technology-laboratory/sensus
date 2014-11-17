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
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private int _commitDelayMS;
        private AutoResetEvent _commitTrigger;
        private Task _commitTask;
        private bool _running;

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
            _commitTrigger = new AutoResetEvent(false);  // delay the first commit
            _running = false;
        }

        protected void StartAsync()
        {
            if (_running)
                throw new InvalidOperationException("Datastore already running.");

            if (Logger.Level >= LoggingLevel.Normal)
                Logger.Log("Starting " + GetType().Name + " data store:  " + Name);

            _running = true;

            _commitTask = Task.Run(() =>
                {
                    while (NeedsToBeRunning)
                    {
                        if (Logger.Level >= LoggingLevel.Debug)
                            Logger.Log(Name + " is about to wait for " + _commitDelayMS + " MS before committing data.");

                        _commitTrigger.WaitOne(_commitDelayMS);

                        if (Logger.Level >= LoggingLevel.Debug)
                            Logger.Log(Name + " is waking up to commit data.");

                        DataCommitted(CommitData(GetDataToCommit()));  // regardless of whether the commit is triggered by the delay or by Stop, we should commit existing data.
                    }

                    if (Logger.Level >= LoggingLevel.Normal)
                        Logger.Log("Exited while-loop for data store " + Name);

                    _running = false;
                });
        }

        protected abstract ICollection<Datum> GetDataToCommit();

        protected abstract ICollection<Datum> CommitData(ICollection<Datum> data);

        protected abstract void DataCommitted(ICollection<Datum> data);


        public async void StopAsync()
        {
            // data stores will automatically stop if NeedsToBeRunning becomes false. however, we might be in the middle of a very long commit delay, in which case the Stop method serves a purpose by immediately triggering a commit and stopping the thread
            if (!_running)
                return;

            if (NeedsToBeRunning)
                throw new InvalidOperationException("DataStore " + Name + " cannot be stopped while it is needed.");

            _running = false;

            if (Logger.Level >= LoggingLevel.Normal)
                Logger.Log("Setting data store " + Name + "'s wait handle within Stop method.");

            await Task.Run(async () =>
                {
                    _commitTrigger.Set();
                    await _commitTask;
                });

            if (Logger.Level >= LoggingLevel.Normal)
                Logger.Log("Data store " + Name + "'s task ended.");
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
