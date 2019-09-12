using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Movement
{
    class LinearAccelerationDatum : Datum
    {
        private double _x;
        private double _y;
        private double _z;
        public LinearAccelerationDatum(DateTimeOffset timestamp,double x, double y, double z) : base(timestamp)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public override string DisplayDetail
        {
            get { return Math.Round(_x, 2) + " (x), " + Math.Round(_y, 2) + " (y), " + Math.Round(_z, 2) + " (z)"; }
        }


        public override object StringPlaceholderValue {
            get
            {
                return "[" + Math.Round(_x, 1) + "," + Math.Round(_y, 1) + "," + Math.Round(_z, 1) + "]";
            }
        }

        /// <summary>
        /// Linear acceleration along the Z axis.
        /// </summary>
        [DoubleProbeTriggerProperty("X Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double X { get => _x; set => _x = value; }

        /// <summary>
        /// Linear acceleration along the Z axis.
        /// </summary>
        [DoubleProbeTriggerProperty("Y Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double Y { get => _y; set => _y = value; }

        /// <summary>
        /// Linear acceleration along the Z axis.
        /// </summary>
        [DoubleProbeTriggerProperty("Z Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double Z { get => _z; set => _z = value; }

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
                   "Z:  " + _z;
        }
    }
}
