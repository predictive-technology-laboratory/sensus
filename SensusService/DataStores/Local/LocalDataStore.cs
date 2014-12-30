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

        protected sealed override ICollection<Datum> GetDataToCommit()
        {
            List<Datum> dataToCommit = new List<Datum>();
            foreach (Probe probe in Protocol.Probes)
            {
                // the collected data object comes directly from the probe, so lock it down before working with it.
                ICollection<Datum> collectedData = probe.GetCollectedData();
                if (collectedData != null)
                    lock (collectedData)
                        if (collectedData.Count > 0)
                            dataToCommit.AddRange(collectedData);
            }

            SensusServiceHelper.Get().Logger.Log("Retrieved " + dataToCommit.Count + " data elements from probes.", LoggingLevel.Verbose);

            return dataToCommit;
        }

        protected sealed override void ProcessCommittedData(ICollection<Datum> committedData)
        {
            SensusServiceHelper.Get().Logger.Log("Clearing " + committedData.Count + " committed data elements from probes.", LoggingLevel.Verbose);

            foreach (Probe probe in Protocol.Probes)
                probe.ClearDataCommittedToLocalDataStore(committedData);
        }

        public abstract ICollection<Datum> GetDataForRemoteDataStore();

        public abstract void ClearDataCommittedToRemoteDataStore(ICollection<Datum> dataCommittedToRemote);
    }
}
