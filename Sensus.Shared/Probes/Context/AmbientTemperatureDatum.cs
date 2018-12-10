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

namespace Sensus.Probes.Context
{
    public class AmbientTemperatureDatum : Datum, IAmbientTemperatureDatum
    {
        private double _degreesCelsius;

        [Anonymizable("Degrees (C)", new Type[] { typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty("Degrees (C)")]
        public double DegreesCelsius
        {
            get
            {
                return _degreesCelsius;
            }
            set
            {
                _degreesCelsius = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Degrees (C):  " + Math.Round(_degreesCelsius, 1);
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the temperature (C).
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return Math.Round(_degreesCelsius, 0);
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private AmbientTemperatureDatum() 
        {
        } 

        public AmbientTemperatureDatum(DateTimeOffset timestamp, double degreesCelcius)
            : base(timestamp)
        {
            _degreesCelsius = degreesCelcius;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Degrees (C):  " + _degreesCelsius;
        }
    }
}
