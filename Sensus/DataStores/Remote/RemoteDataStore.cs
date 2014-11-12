using Sensus.DataStores.Local;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.DataStores.Remote
{
    /// <summary>
    /// Responsible for storing data on remote media.
    /// </summary>
    public abstract class RemoteDataStore : DataStore
    {
        private LocalDataStore _localDataStore;
        private bool _requireWiFi;
        private bool _requireCharging;

        protected LocalDataStore LocalDataStore
        {
            get { return _localDataStore; }
        }

        public bool RequireWiFi
        {
            get { return _requireWiFi; }
            set { _requireWiFi = value; }
        }

        public bool RequireCharging
        {
            get { return _requireCharging; }
            set { _requireCharging = value; }
        }

        public override bool NeedsToBeRunning
        {
            get { return _localDataStore.NeedsToBeRunning; }
        }

        public RemoteDataStore()
        {
            CommitDelayMS = 1000 * 60 * 30;  // every thirty minute by default
        }

        public void Start(LocalDataStore localDataStore)
        {
            _localDataStore = localDataStore;
            Start();
        }

        protected override ICollection<Datum> GetDataToCommit()
        {
            // check if wi-fi required/enabled

            // check if plugged in

            return _localDataStore.GetDataForRemoteDataStore();
        }

        protected override void DataCommitted(ICollection<Datum> data)
        {
            _localDataStore.ClearDataCommittedToRemoteDataStore(data);
        }
    }
}
