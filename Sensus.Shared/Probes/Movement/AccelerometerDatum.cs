// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Movement
{
    /// <summary>
    /// 
    /// Acceleration in x, y, and z directions according to the following coordinate system:
    /// 
    ///   ![Accelerometer Coordiates](/sensus/images/axis_device.png)
    /// 
    ///   * Lying flat on its back, the phone will register an acceleration vector of approximately &lt;0,0,-1&gt;.
    ///   * Lying flat on its face, the phone will register an acceleration vector of approximately &lt;0,0,1&gt;.
    ///   * Lying upright on its left edge, the phone will register an acceleration vector of approximately &lt;-1,0,0&gt;.
    ///   * Lying upright on its right edge, the phone will register an acceleration vector of approximately &lt;1,0,0&gt;.
    ///   * Lying upright on its bottom edge, the phone will register an acceleration vector of approximately &lt;0,-1,0&gt;.
    ///   * Lying upright on its top edge, the phone will register an acceleration vector of approximately &lt;0,1,0&gt;.
    /// 
    /// </summary>
    public class AccelerometerDatum : Datum
    {
        private double _x;
        private double _y;
        private double _z;

        /// <summary>
        /// Acceleration along the X axis.
        /// </summary>
        [DoubleProbeTriggerProperty("X Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Acceleration along the Y axis.
        /// </summary>
        [DoubleProbeTriggerProperty("Y Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Acceleration along the Z axis.
        /// </summary>
        [DoubleProbeTriggerProperty("Z Acceleration")]
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
        /// For JSON deserialization.
        /// </summary>
        private AccelerometerDatum() { }

        public AccelerometerDatum(DateTimeOffset timestamp, double x, double y, double z)
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
