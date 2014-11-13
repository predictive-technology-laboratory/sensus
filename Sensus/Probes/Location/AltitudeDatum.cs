using System;

namespace Sensus.Probes.Location
{
    public class AltitudeDatum : ImpreciseDatum
    {
        private double _altitude;

        public double Altitude
        {
            get { return _altitude; }
        }

        public AltitudeDatum(int probeId, DateTimeOffset timestamp, double accuracy, double altitude)
            : base(probeId, timestamp, accuracy)
        {
            _altitude = altitude;
        }
    }
}
