using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Device
{
    public class ScreenDatum : Datum
    {
        private bool _on;

        public bool On
        {
            get { return _on; }
            set { _on = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _on ? "On" : "Off"; }
        }

        public ScreenDatum(Probe probe, DateTimeOffset timestamp, bool on)
            : base(probe, timestamp)
        {
            _on = on;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "On:  " + _on;
        }
    }
}
