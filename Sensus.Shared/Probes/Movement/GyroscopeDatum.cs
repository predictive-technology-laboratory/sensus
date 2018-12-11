//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Movement
{
    public class GyroscopeDatum : Datum, IGyroscopeDatum
    {
        private double _x;
        private double _y;
        private double _z;

        /// <summary>
        /// Rotation around the X axis (radians/second).
        /// </summary>
        [DoubleProbeTriggerProperty("X Rotation")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Rotation around the Y axis (radians/second).
        /// </summary>
        [DoubleProbeTriggerProperty("Y Rotation")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Rotation around the Z axis (radians/second).
        /// </summary>
        [DoubleProbeTriggerProperty("Z Rotation")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double Z
        {
            get { return _z; }
            set { _z = value; }
        }

        public override string DisplayDetail
        {
            get { return Math.Round(_x, 2) + " (x), " + Math.Round(_y, 2) + " (y), " + Math.Round(_z, 2) + " (z)"; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the rotation vector in [x,y,z] format.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return "[" + Math.Round(_x, 1) + "," + Math.Round(_y, 1) + "," + Math.Round(_z, 1) + "]";
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private GyroscopeDatum() { }

        public GyroscopeDatum(DateTimeOffset timestamp, double x, double y, double z)
            : base(timestamp)
        {
            _x = x;
            _y = y;
            _z = z;
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
