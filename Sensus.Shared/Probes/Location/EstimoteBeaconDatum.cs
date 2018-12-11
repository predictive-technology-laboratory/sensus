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
    public class EstimoteBeaconDatum : Datum, IEstimoteBeaconDatum
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
