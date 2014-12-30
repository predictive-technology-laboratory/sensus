using SensusUI.UiProperties;
using System;

namespace SensusService.Probes
{
    public abstract class ListeningProbe : Probe
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

        protected ListeningProbe()
        {
            _maxDataStoresPerSecond = 1;
        }

        public sealed override void Start()
        {
            lock (this)
            {
                base.Start();

                StartListening();
            }
        }

        protected abstract void StartListening();

        public sealed override void Stop()
        {
            lock (this)
            {
                base.Stop();

                StopListening();
            }
        }

        protected abstract void StopListening();

        protected sealed override void StoreDatum(Datum datum)
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
