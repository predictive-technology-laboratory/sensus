using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
    public abstract class PassiveProbe : Probe, IPassiveProbe
    {
        private int _maxDataStoresPerSecond;
        private DateTime _lastStoreTime;

        [EntryIntegerUiProperty("Max Data / Second:", true)]
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

        protected PassiveProbe()
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
