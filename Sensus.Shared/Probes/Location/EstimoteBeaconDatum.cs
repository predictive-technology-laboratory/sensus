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
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using Newtonsoft.Json;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;

namespace Sensus.Probes.Location
{
    public class EstimoteBeaconDatum : Datum
    {
        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Name { get; set; }

        [DoubleProbeTriggerProperty("Distance (Meters)")]
        [Anonymizable("Distance (Meters)", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double DistanceMeters { get; set; }

        public EstimoteBeaconProximityEvent ProximityEvent { get; set; }

        [StringProbeTriggerProperty("Event Summary")]
        [JsonIgnore]
        public string EventSummary
        {
            get { return ProximityEvent + " " + Name; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get
            {
                return Name + ":  " + ProximityEvent;
            }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private EstimoteBeaconDatum()
        {
        }

        public EstimoteBeaconDatum(DateTimeOffset timestamp, string name, double distanceMeters, EstimoteBeaconProximityEvent proximityEvent)
            : base(timestamp)
        {
            Name = name;
            DistanceMeters = distanceMeters;
            ProximityEvent = proximityEvent;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Name:  " + Name + Environment.NewLine +
                   "Distance (Meters):  " + DistanceMeters + Environment.NewLine +
                   "Proximity Event:  " + ProximityEvent;
        }
    }
}