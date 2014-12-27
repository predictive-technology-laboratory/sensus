using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Context
{
    public class LightDatum : Datum
    {
        private float _brightness;

        public float Brightness
        {
            get { return _brightness; }
            set { _brightness = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return "Brightness:  " + _brightness; }
        }

        public LightDatum(Probe probe, DateTimeOffset timestamp, float brightness)
            : base(probe, timestamp)
        {
            _brightness = brightness;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Brightness:  " + _brightness;
        }
    }
}
