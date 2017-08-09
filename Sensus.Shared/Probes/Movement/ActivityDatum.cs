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

namespace Sensus.Probes.Movement
{
    public class ActivityDatum : Datum
    {
        [ListProbeTriggerProperty(new object[] { Activities.InVehicle, Activities.OnBicycle, Activities.OnFoot, Activities.Running, Activities.Still, Activities.Tilting, Activities.Unknown, Activities.Walking })]
        public Activities Activity { get; set; }

        public ActivityState State { get; set; }

        public override string DisplayDetail
        {
            get
            {
                return "Activity:  " + Activity + " (" + State + ")";
            }
        }

        /// <summary>
        /// For JSON deserialization 
        /// </summary>
        private ActivityDatum()
        {
        }

        public ActivityDatum(DateTimeOffset timestamp, Activities activity, ActivityState state)
            : base(timestamp)
        {
            Activity = activity;
            State = state;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Activity:  " + Activity + Environment.NewLine +
                   "State:  " + State;
        }
    }
}
