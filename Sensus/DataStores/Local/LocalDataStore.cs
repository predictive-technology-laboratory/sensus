using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.DataStores.Local
{
    /// <summary>
    /// Responsible for transferring data from probes to local media.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        private Protocol _protocol;

        protected Protocol Protocol
        {
            get { return _protocol; }
        }

        public override bool NeedsToBeRunning
        {
            get { return _protocol.Running; }
        }

        public LocalDataStore()
        {
            CommitDelayMS = 5000;
        }

        public void StartAsync(Protocol protocol)
        {
            _protocol = protocol;
            StartAsync();
        }

        protected override ICollection<Datum> GetDataToCommit()
        {
            List<Datum> dataToCommit = new List<Datum>();
            foreach (Probe probe in _protocol.Probes)
            {
                ICollection<Datum> collectedData = probe.GetCollectedData();
                lock (collectedData)
                    if (collectedData.Count > 0)
                        dataToCommit.AddRange(collectedData);
            }

            return dataToCommit;
        }

        protected override void DataCommitted(ICollection<Datum> data)
        {
            foreach (Probe probe in _protocol.Probes)
                probe.ClearCommittedData(data);
        }

        public abstract ICollection<Datum> GetDataForRemoteDataStore();

        public abstract void ClearDataCommittedToRemoteDataStore(ICollection<Datum> data);
    }
}
