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
        public LocalDataStore()
        {
            CommitDelayMS = 5000;
        }

        protected override ICollection<Datum> GetDataToCommit()
        {
            List<Datum> dataToCommit = new List<Datum>();
            foreach (Probe probe in Protocol.Probes)
            {
                ICollection<Datum> collectedData = probe.GetCollectedData();
                if (collectedData != null)
                    lock (collectedData)
                        if (collectedData.Count > 0)
                            dataToCommit.AddRange(collectedData);
            }

            if (App.LoggingLevel >= LoggingLevel.Normal)
                App.Get().SensusService.Log("Retrieved " + dataToCommit.Count + " data elements from probes.");

            return dataToCommit;
        }

        protected override void DataCommitted(ICollection<Datum> data)
        {
            if (App.LoggingLevel >= LoggingLevel.Normal)
                App.Get().SensusService.Log("Clearing " + data.Count + " committed data elements from probes.");

            foreach (Probe probe in Protocol.Probes)
                probe.ClearCommittedData(data);
        }

        public abstract ICollection<Datum> GetDataForRemoteDataStore();

        public abstract void ClearDataCommittedToRemoteDataStore(ICollection<Datum> data);
    }
}
