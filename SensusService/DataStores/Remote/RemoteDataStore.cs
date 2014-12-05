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

        [OnOffUiProperty("Require WiFi:", true)]
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

        [OnOffUiProperty("Require Charging:", true)]
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
                if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Verbose)
                    SensusServiceHelper.Get().Log("Required WiFi but device WiFi is not connected.");

                return new List<Datum>();
            }
            else if (_requireCharging && !SensusServiceHelper.Get().IsCharging)
            {
                if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Verbose)
                    SensusServiceHelper.Get().Log("Required charging but device is not charging.");

                return new List<Datum>();
            }

            ICollection<Datum> localData = Protocol.LocalDataStore.GetDataForRemoteDataStore();

            if (SensusServiceHelper.LoggingLevel >= LoggingLevel.Verbose)
                SensusServiceHelper.Get().Log("Retrieved " + localData.Count + " data elements from local data store.");

            return localData;
        }

        protected override void ProcessCommittedData(ICollection<Datum> committedData)
        {
            Protocol.LocalDataStore.ClearDataCommittedToRemoteDataStore(committedData);
        }
    }
}
