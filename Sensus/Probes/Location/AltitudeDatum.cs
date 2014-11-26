using System;

namespace Sensus.Probes.Location
{
    public class AltitudeDatum : ImpreciseDatum
    {
        private double _altitude;

        public override string DisplayDetail
        {
            get { return Math.Round(_altitude, 0) + " feet"; }
        }

        public double Altitude
        {
            get { return _altitude; }
        }

        public AltitudeDatum(int probeId, DateTimeOffset timestamp, double accuracy, double altitude)
            : base(probeId, timestamp, accuracy)
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
