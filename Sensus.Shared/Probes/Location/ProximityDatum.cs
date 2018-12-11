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
using Newtonsoft.Json;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// The proximity sensor is usually used to determine how far away a person's head is 
    /// from the face of a handset device (for example, when a user is making or receiving 
    /// a phone call). Most proximity sensors return the absolute distance, in cm, but 
    /// some return only near and far values. The behavior of this probe differs on Android
    /// and iOS, so be sure to read the relevant documentation for those probes.
    /// </summary>
    public class ProximityDatum : Datum, IProximityDatum
    {
        private double _distance;
        private double _maxDistance;

        /// <summary>
        /// Most proximity sensors return the absolute distance, in cm, 
        /// but some return only near and far values.
        /// </summary>
        [DoubleProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double Distance
        {
            get { return _distance; }
            set { _distance = value; }
        }

        [DoubleProbeTriggerProperty("Max. Distance")]
        [Anonymizable("Max. Distance", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double MaxDistance
        {
            get { return _maxDistance; }
            set { _maxDistance = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return Math.Round(_distance, 1) + " (max. distance: " + Math.Round(_maxDistance) + ")"; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the distance the phone is from an object 
        /// and the maximum distance that the phone reports.  If the distance equals the
        /// maximum distance then that is a sign that the phone only reports near and far and
        /// doesn't report an accurate distance.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return Math.Round(_distance, 1);
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private ProximityDatum() { }

        public ProximityDatum(DateTimeOffset timestamp, double distance, double maxDistance)
            : base(timestamp)
        {
            _distance = distance;
            _maxDistance = maxDistance;
        }


        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Distance:  " + _distance + Environment.NewLine +
                   "Max. Distance: " + _maxDistance;
        }
    }
}
