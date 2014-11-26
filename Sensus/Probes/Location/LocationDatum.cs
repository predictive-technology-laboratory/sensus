using System;

namespace Sensus.Probes.Location
{
    public class LocationDatum : ImpreciseDatum
    {
        private double _latitude;
        private double _longitude;

        public override string DisplayDetail
        {
            get { return _latitude + " (lat), " + _longitude + " (lon)"; }
        }

        public double Latitude
        {
            get { return _latitude; }
        }

        public double Longitude
        {
            get { return _longitude; }
        }

        public LocationDatum(int probeId, DateTimeOffset timestamp, double accuracy, double latitude, double longitude)
            : base(probeId, timestamp, accuracy)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Latitude:  " + _latitude + Environment.NewLine +
                   "Longitude:  " + _longitude;
        }
    }
}
