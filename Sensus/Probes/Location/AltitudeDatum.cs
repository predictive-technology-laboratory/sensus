using Newtonsoft.Json;
using System;

namespace Sensus.Probes.Location
{
    public class AltitudeDatum : ImpreciseDatum
    {
        private double _altitude;

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return Math.Round(_altitude, 0) + " feet"; }
        }

        public double Altitude
        {
            get { return _altitude; }
            set { _altitude = value; }
        }

        public AltitudeDatum(int userId, int probeId, long timestampTicks, double accuracy, double altitude)
            : base(userId, probeId, timestampTicks, accuracy)
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
