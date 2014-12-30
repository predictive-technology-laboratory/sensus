using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Context
{
    public class SoundDatum : Datum
    {
        private double _decibels;

        public double Decibels
        {
            get { return _decibels; }
            set { _decibels = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return Math.Round(_decibels, 0) + " (db)"; }
        }

        public SoundDatum(Probe probe, DateTimeOffset timestamp, double decibels)
            : base(probe, timestamp)
        {
            _decibels = decibels;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Decibels:  " + _decibels;
        }
    }
}
