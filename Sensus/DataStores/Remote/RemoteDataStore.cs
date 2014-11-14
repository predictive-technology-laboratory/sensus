using Sensus.DataStores.Local;
using System.Collections.Generic;

namespace Sensus.DataStores.Remote
{
    /// <summary>
    /// Responsible for transferring data from a local data store to remote media.
    /// </summary>
    public abstract class RemoteDataStore : DataStore
    {
        private LocalDataStore _localDataStore;
        private bool _requireWiFi;
        private bool _requireCharging;

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
            _requireWiFi = true;
            _requireCharging = true;
            CommitDelayMS = 1000 * 60 * 30;  // every thirty minutes by default
        }

        public void StartAsync(LocalDataStore localDataStore)
        {
            _localDataStore = localDataStore;
            StartAsync();
        }

        protected override ICollection<Datum> GetDataToCommit()
        {
            // TODO:  check if wi-fi required/connected

            // TODO:  check if power required/connected

            return _localDataStore.GetDataForRemoteDataStore();
        }

        protected override void DataCommitted(ICollection<Datum> data)
        {
            _localDataStore.ClearDataCommittedToRemoteDataStore(data);
        }
    }
}
