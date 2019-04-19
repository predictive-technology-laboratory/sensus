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
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sensus;
using Sensus.Probes.Movement;
using System.Linq;
using Sensus.Probes.Location;

namespace ExampleSensingAgent
{
    public class ExampleAccelerationSensingAgent : SensingAgent
    {
        private double _averageLinearMagnitudeThreshold;

        public override string Id => "Acceleration";

        public override string Description => "ALM / Proximity";

        public ExampleAccelerationSensingAgent()
        {
            _averageLinearMagnitudeThreshold = 0.1;
        }

        public override async Task SetPolicyAsync(JObject policy)
        {
            await base.SetPolicyAsync(policy);

            _averageLinearMagnitudeThreshold = double.Parse(policy.GetValue("alm-threshold").ToString());
        }

        protected override void UpdateObservedData(Dictionary<Type, List<IDatum>> typeData)
        {
            foreach (Type type in typeData.Keys)
            {
                List<IDatum> data = typeData[type];

                while (data.Count > 100)
                {
                    data.RemoveAt(0);
                }
            }
        }

        protected override bool ObservedDataMeetControlCriterion(List<IDatum> data)
        {
            bool criterionMet = false;

            if (data.Count > 0)
            {
                Type dataType = data[0].GetType();

                if (dataType.GetInterfaces().Contains(typeof(IProximityDatum)))
                {
                    IProximityDatum mostRecentProximityDatum = data.Last() as IProximityDatum;

                    if (mostRecentProximityDatum.Distance < mostRecentProximityDatum.MaxDistance)
                    {
                        criterionMet = true;
                    }
                }

                if (!criterionMet && dataType.GetInterfaces().Contains(typeof(IAccelerometerDatum)))
                {
                    double averageLinearMagnitude = data.Cast<IAccelerometerDatum>().Average(accelerometerDatum => Math.Sqrt(Math.Pow(accelerometerDatum.X, 2) + Math.Pow(accelerometerDatum.Y, 2) + Math.Pow(accelerometerDatum.Z, 2)));

                    // acceleration values include gravity. thus, a stationary device will register 1 on one of the axes.
                    // use absolute deviation from 1 as the criterion value with which to compare the threshold.
                    if (Math.Abs(averageLinearMagnitude - 1) >= _averageLinearMagnitudeThreshold)
                    {
                        criterionMet = true;
                    }
                }
            }

            return criterionMet;
        }
    }
}
