using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Location
{
    public class LocationDatum : ImpreciseDatum
    {
        private double _latitude;
        private double _longitude;

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
    }
}
