using SensusUI.UiProperties;
using System.Collections.Generic;

namespace SensusService.DataStores.Remote
{
    /// <summary>
    /// Responsible for transferring data from a local data store to remote media.
    /// </summary>
    public abstract class RemoteDataStore : DataStore
    {
        private bool _requireWiFi;
        private bool _requireCharging;

        [OnOffUiProperty("Require WiFi:", true, int.MaxValue)]
        public bool RequireWiFi
        {
            get { return _requireWiFi; }
            set
            {
                if (value != _requireWiFi)
                {
                    _requireWiFi = value;
                    OnPropertyChanged();
                }
            }
        }

        [OnOffUiProperty("Require Charging:", true, int.MaxValue)]
        public bool RequireCharging
        {
            get { return _requireCharging; }
            set
            {
                if (value != _requireCharging)
                {
                    _requireCharging = value;
                    OnPropertyChanged();
                }
            }
        }

        public RemoteDataStore()
        {
            _requireWiFi = true;
            _requireCharging = true;

#if DEBUG
            CommitDelayMS = 10000;  // 10 seconds...so we can see debugging output quickly
#else
            CommitDelayMS = 1000 * 60 * 30;  // every thirty minutes
#endif
        }

        protected override ICollection<Datum> GetDataToCommit()
        {
            if (_requireWiFi && !SensusServiceHelper.Get().WiFiConnected)
            {
                SensusServiceHelper.Get().Logger.Log("Required WiFi but device WiFi is not connected.", LoggingLevel.Verbose);
                return new List<Datum>();
            }
            else if (_requireCharging && !SensusServiceHelper.Get().IsCharging)
            {
                SensusServiceHelper.Get().Logger.Log("Required charging but device is not charging.", LoggingLevel.Verbose);
                return new List<Datum>();
            }

            ICollection<Datum> localData = Protocol.LocalDataStore.GetDataForRemoteDataStore();

            SensusServiceHelper.Get().Logger.Log("Retrieved " + localData.Count + " data elements from local data store.", LoggingLevel.Verbose);

            return localData;
        }

        protected override void ProcessCommittedData(ICollection<Datum> committedData)
        {
            Protocol.LocalDataStore.ClearDataCommittedToRemoteDataStore(committedData);
        }

        public abstract void UploadProtocolReport(ProtocolReport report);
    }
}
