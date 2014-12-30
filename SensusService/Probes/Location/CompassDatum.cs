using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Location
{
    public class CompassDatum : Datum
    {
        private double _heading;

        public double Heading
        {
            get { return _heading; }
            set { _heading = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return Math.Round(_heading, 0) + " degrees from magnetic north"; }
        }

        public CompassDatum(Probe probe, DateTimeOffset timestamp, double heading)
            : base(probe, timestamp)
        {
            _heading = heading;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Heading:  " + _heading;
        }
    }
}
