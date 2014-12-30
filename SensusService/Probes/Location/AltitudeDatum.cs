using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Location
{
    public class AltitudeDatum : ImpreciseDatum
    {
        private double _altitude;

        public double Altitude
        {
            get { return _altitude; }
            set { _altitude = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return Math.Round(_altitude, 0) + " feet"; }
        }

        public AltitudeDatum(Probe probe, DateTimeOffset timestamp, double accuracy, double altitude)
            : base(probe, timestamp, accuracy)
        {
            _altitude = altitude;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Altitude:  " + _altitude + " feet";
        }
    }
}
