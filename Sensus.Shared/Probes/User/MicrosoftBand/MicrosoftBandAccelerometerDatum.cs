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

namespace Sensus.Probes.User.MicrosoftBand
{
    public class MicrosoftBandAccelerometerDatum : Datum
    {
        private double _x;
        private double _y;
        private double _z;

        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double X
        {
            get
            {
                return _x;
            }

            set
            {
                _x = value;
            }
        }

        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double Y
        {
            get
            {
                return _y;
            }

            set
            {
                _y = value;
            }
        }

        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double Z
        {
            get
            {
                return _z;
            }

            set
            {
                _z = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "X:  " + Math.Round(_x, 2) + ", Y:  " + Math.Round(_y, 2) + ", Z:  " + Math.Round(_z, 2);
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the acceleration vector [x,y,z].
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return "[" + Math.Round(_x, 2) + "," + Math.Round(_y, 2) + "," + Math.Round(_z, 2) + "]";
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandAccelerometerDatum()
        {
        }

        public MicrosoftBandAccelerometerDatum(DateTimeOffset timestamp, double x, double y, double z)
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