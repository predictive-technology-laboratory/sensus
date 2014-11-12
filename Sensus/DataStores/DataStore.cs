using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sensus.DataStores
{
    /// <summary>
    /// An abstract repository for probed data.
    /// </summary>
    public abstract class DataStore
    {
        private int _commitDelayMS;
        private AutoResetEvent _commitTrigger;
        private Thread _thread;

        public int CommitDelayMS
        {
            get { return _commitDelayMS; }
            set { _commitDelayMS = value; }
        }

        public abstract bool NeedsToBeRunning { get; }

        public DataStore()
        {
            _commitDelayMS = 10000;
            _commitTrigger = new AutoResetEvent(false);
        }

        public abstract void Test();

        protected void Start()
        {
            _thread = new Thread(new ThreadStart(() =>
                {
                    while(NeedsToBeRunning)
                    {
                        _commitTrigger.WaitOne(_commitDelayMS);

                        if (NeedsToBeRunning)
                            DataCommitted(CommitData(GetDataToCommit()));
                    }
                }));

            _thread.Start();
        }

        public void Commit()
        {
            _commitTrigger.Set();
        }

        protected abstract ICollection<Datum> GetDataToCommit();

        protected abstract ICollection<Datum> CommitData(ICollection<Datum> data);

        protected abstract void DataCommitted(ICollection<Datum> data);


        public void Stop()
        {
            if (NeedsToBeRunning)
                throw new InvalidOperationException("DataStore cannot be stopped while it is needed.");

            _commitTrigger.Set();
            _thread.Join();
        }
    }
}
