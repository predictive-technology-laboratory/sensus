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

namespace Sensus.Probes.User.MicrosoftBand
{
    public class MicrosoftBandGyroscopeDatum : Datum
    {
        private double _angularX;
        private double _angularY;
        private double _angularZ;

        [Anonymizable("X:", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double AngularX
        {
            get
            {
                return _angularX;
            }

            set
            {
                _angularX = value;
            }
        }

        [Anonymizable("Y:", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double AngularY
        {
            get
            {
                return _angularY;
            }

            set
            {
                _angularY = value;
            }
        }

        [Anonymizable("Z:", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double AngularZ
        {
            get
            {
                return _angularZ;
            }

            set
            {
                _angularZ = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Angular X:  " + Math.Round(_angularX, 2) + ", Angular Y:  " + Math.Round(_angularY, 2) + ", Angular Z:  " + Math.Round(_angularZ, 2);
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the gyroscope angular [x,y,z].
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return "[" + Math.Round(_angularX, 2) + "," + Math.Round(_angularY, 2) + "," + Math.Round(_angularZ, 2) + "]";
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandGyroscopeDatum()
        {
        }

        public MicrosoftBandGyroscopeDatum(DateTimeOffset timestamp, double angularX, double angularY, double angularZ)
            : base(timestamp)
        {
            _angularX = angularX;
            _angularY = angularY;
            _angularZ = angularZ;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Angular X:  " + _angularX + Environment.NewLine +
                   "Angular Y:  " + _angularY + Environment.NewLine +
                   "Angular Z:  " + _angularZ;
        }
    }
}
