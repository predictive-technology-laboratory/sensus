using Sensus.Protocols;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sensus.DataStores.Local
{
    /// <summary>
    /// Responsible for storing data on the device's local media.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        private int _commitDelayMS;
        private AutoResetEvent _commitTrigger;

        public int CommitDelayMS
        {
            get { return _commitDelayMS; }
            set { _commitDelayMS = value; }
        }

        protected AutoResetEvent CommitTrigger
        {
            get { return _commitTrigger; }
        }

        public LocalDataStore()
            : base()
        {
            _commitDelayMS = 1000;
            _commitTrigger = new AutoResetEvent(false);
        }
        
        public abstract void Start(Protocol protocol);

        public abstract void Stop();
    }
}
