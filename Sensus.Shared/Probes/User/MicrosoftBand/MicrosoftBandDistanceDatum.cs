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
using Microsoft.Band.Portable.Sensors;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.User.MicrosoftBand
{
    public class MicrosoftBandDistanceDatum : Datum
    {
        private double _totalDistance;
        private MotionType _motionType;

        [Anonymizable("Total Distance:", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double TotalDistance
        {
            get
            {
                return _totalDistance;
            }

            set
            {
                _totalDistance = value;
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public MotionType MotionType
        {
            get
            {
                return _motionType;
            }

            set
            {
                _motionType = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Total Distance:  " + Math.Round(_totalDistance, 1) + ", Motion Type:  " + _motionType;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the total distance.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return Math.Round(_totalDistance, 1);
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandDistanceDatum()
        {
        }

        public MicrosoftBandDistanceDatum(DateTimeOffset timestamp, double totalDistance, MotionType motionType)
            : base(timestamp)
        {
            _totalDistance = totalDistance;
            _motionType = motionType;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Total Distance:  " + _totalDistance + Environment.NewLine +
                   "Motion Type:  " + _motionType;
        }
    }
}
