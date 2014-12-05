using SensusService.Probes;
using System.Collections.Generic;

namespace SensusService.DataStores.Local
{
    /// <summary>
    /// Responsible for transferring data from probes to local media.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        public LocalDataStore()
        {
#if DEBUG
            CommitDelayMS = 5000;  // 5 seconds...so we can see debugging output quickly
#else
            CommitDelayMS = 60000;
#endif
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

            if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Verbose)
                SensusServiceHelper.Get().Log("Retrieved " + dataToCommit.Count + " data elements from probes.");

            return dataToCommit;
        }

        protected override void ProcessCommittedData(ICollection<Datum> committedData)
        {
            if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Verbose)
                SensusServiceHelper.Get().Log("Clearing " + committedData.Count + " committed data elements from probes.");

            foreach (Probe probe in Protocol.Probes)
                probe.ClearCommittedData(committedData);
        }

        public abstract ICollection<Datum> GetDataForRemoteDataStore();

        public abstract void ClearDataCommittedToRemoteDataStore(ICollection<Datum> dataCommittedToRemote);
    }
}
