using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

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
        private Thread _thread;
        private bool _running;

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

        public DataStore()
        {
            _name = GetType().Name;
            _commitDelayMS = 10000;
            _commitTrigger = new AutoResetEvent(false);  // delay commits for a period
            _running = false;
        }

        public abstract void Test();

        protected void Start()
        {
            if (_running)
                throw new InvalidOperationException("Datastore already running.");

            _running = true;

            _thread = new Thread(new ThreadStart(() =>
                {
                    while (NeedsToBeRunning)
                    {
                        _commitTrigger.WaitOne(_commitDelayMS);
                        DataCommitted(CommitData(GetDataToCommit()));  // regardless of whether the commit is triggered by the delay or by Stop, we should commit existing data.
                    }

                    _running = false;
                }));

            _thread.Start();
        }

        protected abstract ICollection<Datum> GetDataToCommit();

        protected abstract ICollection<Datum> CommitData(ICollection<Datum> data);

        protected abstract void DataCommitted(ICollection<Datum> data);


        public void Stop()
        {
            if (!_running)
                throw new InvalidOperationException("Datastore already stopped.");

            if (NeedsToBeRunning)
                throw new InvalidOperationException("DataStore cannot be stopped while it is needed.");

            _commitTrigger.Set();
            _thread.Join();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
