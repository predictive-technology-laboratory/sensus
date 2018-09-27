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
using Newtonsoft.Json.Converters;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Event proximity for named beacons at specified distance thresholds.
    /// </summary>
    public class EstimoteBeaconDatum : Datum
    {
        /// <summary>
        /// The tag assigned to the beacon.
        /// </summary>
        /// <value>The name of the beacon.</value>
        [StringProbeTriggerProperty("Beacon Tag")]
        [Anonymizable("Beacon Tag:", typeof(StringHashAnonymizer), false)]
        public string BeaconTag { get; set; }

        /// <summary>
        /// Name of the proximity event.
        /// </summary>
        /// <value>The name of the event.</value>
        [StringProbeTriggerProperty("Event Name")]
        [Anonymizable("Event Name:", typeof(StringHashAnonymizer), false)]
        public string EventName { get; set; }

        /// <summary>
        /// Proximity detection threshold.
        /// </summary>
        /// <value>The distance in meters.</value>
        [DoubleProbeTriggerProperty("Distance (Meters)")]
        [Anonymizable("Distance (Meters):", new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public double DistanceMeters { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EstimoteBeaconProximityEvent ProximityEvent { get; set; }

        [StringProbeTriggerProperty("Entered/Exited [Event Name]")]
        [JsonIgnore]
        public string EventSummary
        {
            get { return ProximityEvent + " " + EventName; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get
            {
                return ProximityEvent + " " + EventName;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the beacon name.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return BeaconTag;
            }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private EstimoteBeaconDatum()
        {
        }

        public EstimoteBeaconDatum(DateTimeOffset timestamp, EstimoteBeacon beacon, EstimoteBeaconProximityEvent proximityEvent)
            : base(timestamp)
        {
            BeaconTag = beacon.Tag;
            EventName = beacon.EventName;
            DistanceMeters = beacon.ProximityMeters;
            ProximityEvent = proximityEvent;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Beacon Tag:  " + BeaconTag + Environment.NewLine +
                   "Event Name:  " + EventName + Environment.NewLine +
                   "Distance (Meters):  " + DistanceMeters + Environment.NewLine +
                   "Proximity Event:  " + ProximityEvent;
        }
    }
}