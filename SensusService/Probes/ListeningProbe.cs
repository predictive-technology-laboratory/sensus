using SensusUI.UiProperties;
using System;

namespace SensusService.Probes
{
    public abstract class ListeningProbe : Probe, IListeningProbe
    {
        private int _maxDataStoresPerSecond;

        [EntryIntegerUiProperty("Max Data / Second:", true, int.MaxValue)]
        public int MaxDataStoresPerSecond
        {
            get { return _maxDataStoresPerSecond; }
            set
            {
                if (value != _maxDataStoresPerSecond)
                {
                    _maxDataStoresPerSecond = value;
                    OnPropertyChanged();
                }
            }
        }

        protected override ProbeController DefaultController
        {
            get { return new ListeningProbeController(this); }
        }

        protected ListeningProbe()
        {
            _maxDataStoresPerSecond = 1;
        }

        public abstract void StartListening();

        public abstract void StopListening();

        public override void StoreDatum(Datum datum)
        {
            DateTimeOffset lastStoreTime = DateTimeOffset.MinValue;
            if (MostRecentlyStoredDatum != null)
                lastStoreTime = MostRecentlyStoredDatum.Timestamp;

            float storesPerSecond = 1 / (float)(DateTime.Now - lastStoreTime).TotalSeconds;
            if (storesPerSecond <= _maxDataStoresPerSecond)
                base.StoreDatum(datum);
        }
    }
}
