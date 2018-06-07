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
using Newtonsoft.Json.Converters;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Movement
{
    /// <summary>
    /// Activity the user is engaged in, as inferred from the phone's sensors.
    /// </summary>
    public class ActivityDatum : Datum
    {
        /// <summary>
        /// The type of activity (see <see cref="Activities"/>).
        /// </summary>
        /// <value>The activity.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public Activities Activity { get; set; }

        /// <summary>
        /// The phase of the <see cref="Activity"/> (see <see cref="ActivityPhase"/>).
        /// </summary>
        /// <value>The phase.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ActivityPhase Phase { get; set; }

        /// <summary>
        /// The state of the <see cref="Phase"/> (see <see cref="ActivityState"/>).
        /// </summary>
        /// <value>The state.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ActivityState State { get; set; }

        /// <summary>
        /// The confidence level of the estimated activity information.
        /// </summary>
        /// <value>The confidence.</value>
        public ActivityConfidence Confidence { get; set; }

        /// <summary>
        /// Gets the activity the user is starting to engage in. If there is such an activity, it will be one of <see cref="Activities"/>. If there
        /// is not an activity starting, then the value will be <see cref="Activities.Unknown"/>.
        /// </summary>
        /// <value>The current activity.</value>
        [ListProbeTriggerProperty(new object[] { Activities.InVehicle, Activities.OnBicycle, Activities.OnFoot, Activities.Running, Activities.Still, Activities.Tilting, Activities.Walking, Activities.Unknown })]
        [JsonIgnore]
        public Activities ActivityStarting
        {
            get
            {
                return Phase == ActivityPhase.Starting && State == ActivityState.Active ? Activity : Activities.Unknown;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return Phase + " " + Activity + " (" + State + ")";
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is <see cref="ActivityStarting"/>.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return ActivityStarting;
            }
        }

        /// <summary>
        /// For JSON deserialization 
        /// </summary>
        private ActivityDatum()
        {
        }

        public ActivityDatum(DateTimeOffset timestamp, Activities activity, ActivityPhase phase, ActivityState state, ActivityConfidence confidence)
            : base(timestamp)
        {
            Activity = activity;
            Phase = phase;
            State = state;
            Confidence = confidence;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Activity:  " + Activity + Environment.NewLine +
                   "Phase:  " + Phase + Environment.NewLine +
                   "State:  " + State + Environment.NewLine +
                   "Confidence:  " + Confidence;
        }
    }
}