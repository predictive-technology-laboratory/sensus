using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Movement
{
    class PedometerDatum : Datum
    {
        double _steps;

        public PedometerDatum(DateTimeOffset timestamp, double Steps) : base(timestamp)
        {
            _steps = Steps;
        }

        public override string DisplayDetail
        {
            get { return "Steps:  " + Math.Round(_steps, 2); }
        }

        public override object StringPlaceholderValue
        {
            get { return "Steps:  " + Math.Round(_steps, 2); }
        }

        public double Steps { get => _steps; set => _steps = value; }

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
                   "Steps:  " + _steps;
        }
    }
}
