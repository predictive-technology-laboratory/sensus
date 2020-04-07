using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Movement
{
    class AttitudeDatum : Datum, IAttitudeDatum
    {
        double _x;
        double _y;
        double _z;
        double _w;
          
         public AttitudeDatum(DateTimeOffset timestamp, double x,double y,double z, double w) : base(timestamp)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
        }

        public override string DisplayDetail {
            get { return Math.Round(_x, 2) + " (x), " + Math.Round(_y, 2) + " (y), " + Math.Round(_z, 2) + " (z)" + Math.Round(_w, 2) + " (w) " ; }
        }

        public override object StringPlaceholderValue {
            get
            {
                return "[" + Math.Round(_x, 1) + "," + Math.Round(_y, 1) + "," + Math.Round(_z, 1) + "," + Math.Round(_w, 1) + "]";
            }

        }

        public double X { get => _x; set => _x = value; }
        public double Y { get => _y; set => _y = value; }
        public double Z { get => _z; set => _z = value; }
        public double W { get => _w; set => _w = value; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                  "X:  " + _x + Environment.NewLine +
                  "Y:  " + _y + Environment.NewLine +
                  "Z:  " + _z + Environment.NewLine+
                  "W:  "+ _w;
        }
    }
}
