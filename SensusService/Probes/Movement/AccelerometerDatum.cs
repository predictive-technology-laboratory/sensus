using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService.Probes.Movement
{
    public class AccelerometerDatum : Datum
    {
        private float _x;
        private float _y;
        private float _z;

        public float X
        {
            get { return _x; }
            set { _x = value; }
        }

        public float Y
        {
            get { return _y; }
            set { _y = value; }
        }

        public float Z
        {
            get { return _z; }
            set { _z = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return Math.Round(_x, 2) + " (x), " + Math.Round(_y, 2) + " (y), " + Math.Round(_z, 2) + " (z)"; }
        }

        public AccelerometerDatum(Probe probe, DateTimeOffset timestamp, float x, float y, float z)
            : base(probe, timestamp)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "x:  " + _x + Environment.NewLine +
                   "y:  " + _y + Environment.NewLine +
                   "z:  " + _z;
        }
    }
}
