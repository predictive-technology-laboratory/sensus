using Newtonsoft.Json;
using System;

namespace Sensus.Probes.Location
{
    public class CompassDatum : Datum
    {
        private double _heading;

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return Math.Round(_heading, 0) + " degrees from magnetic north"; }
        }

        public double Heading
        {
            get { return _heading; }
            set { _heading = value; }
        }

        public CompassDatum(int userId, int probeId, DateTimeOffset timestamp, double heading)
            : base(userId, probeId, timestamp)
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
