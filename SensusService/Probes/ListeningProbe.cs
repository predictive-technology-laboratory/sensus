using SensusUI.UiProperties;
using System;

namespace SensusService.Probes
{
    public abstract class ListeningProbe : Probe, IListeningProbe
    {
        private int _maxDataStoresPerSecond;
        private DateTime _lastStoreTime;

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
            _lastStoreTime = DateTime.MinValue;
        }

        public abstract void StartListening();

        public abstract void StopListening();

        public override void StoreDatum(Datum datum)
        {
            float dataPerSecond = 1 / (float)(DateTime.Now - _lastStoreTime).TotalSeconds;
            if (dataPerSecond < _maxDataStoresPerSecond)
            {
                base.StoreDatum(datum);
                _lastStoreTime = DateTime.Now;
            }
        }
    }
}
