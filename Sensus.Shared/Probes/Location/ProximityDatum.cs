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