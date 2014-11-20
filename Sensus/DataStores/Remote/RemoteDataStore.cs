using Sensus.DataStores.Local;
using System;
using System.Collections.Generic;

namespace Sensus.DataStores.Remote
{
    /// <summary>
    /// Responsible for transferring data from a local data store to remote media.
    /// </summary>
    [Serializable]
    public abstract class RemoteDataStore : DataStore
    {
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
            get { return Protocol.LocalDataStore.NeedsToBeRunning; }
        }

        public RemoteDataStore()
        {
            _requireWiFi = true;
            _requireCharging = true;
            CommitDelayMS = 1000 * 60 * 30;  // every thirty minutes by default
        }

        protected override ICollection<Datum> GetDataToCommit()
        {
            // TODO:  check if wi-fi required/connected

            // TODO:  check if power required/connected

            return Protocol.LocalDataStore.GetDataForRemoteDataStore();
        }

        protected override void DataCommitted(ICollection<Datum> data)
        {
            Protocol.LocalDataStore.ClearDataCommittedToRemoteDataStore(data);
        }
    }
}
