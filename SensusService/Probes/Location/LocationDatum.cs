using Newtonsoft.Json;
using System;

namespace SensusService.Probes.Location
{
    public class LocationDatum : ImpreciseDatum
    {
        private double _latitude;
        private double _longitude;

        public double Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        public double Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _latitude + " (lat), " + _longitude + " (lon)"; }
        }

        public LocationDatum(Probe probe, DateTimeOffset timestamp, double accuracy, double latitude, double longitude)
            : base(probe, timestamp, accuracy)
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
