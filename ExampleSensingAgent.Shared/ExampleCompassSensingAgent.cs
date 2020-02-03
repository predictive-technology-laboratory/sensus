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
using Sensus.Probes.Location;
using Sensus.Adaptation;
using System.Linq;
using Sensus.Probes;
using System.Threading;

namespace ExampleSensingAgent
{
    /// <summary>
    /// Example compass sensing agent. Demonstrates concepts related to control criterion checking as well as 
    /// sensing control, in particular temporary device keep-awake and increased sampling rates.
    /// </summary>
    public class ExampleCompassSensingAgent : SensingAgent
    {
        #region Private Properties
        private double TargetHeadingCenter {get; set;}
        private double TargetHeadingRange { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// The SensingAgent parameterless constructor required for run time initialization.
        /// </summary>
        public ExampleCompassSensingAgent(): base("CompassSensingAgent", "Compass", TimeSpan.FromSeconds(5))
        {
            TargetHeadingCenter = 0;
            TargetHeadingRange  = 45;
        }
        #endregion

        #region Protected Overrides
        protected override Task ProtectedSetPolicyAsync(JObject policy)
        {
            TargetHeadingCenter = double.Parse(policy["thc-degrees"].ToString());
            TargetHeadingRange  = double.Parse(policy["thr-degrees"].ToString());

            return Task.CompletedTask;
        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData)
        {
            var compassData = GetObservedData<ICompassDatum>().Cast<ICompassDatum>();

            return compassData.Any() && HeadingWithinTargetRange(compassData.Last().Heading);
        }

        protected override async Task OnStateChangedAsync(SensingAgentState previousState, SensingAgentState currentState, CancellationToken cancellationToken)
        {
            await base.OnStateChangedAsync(previousState, currentState, cancellationToken);

            if (currentState == SensingAgentState.OpportunisticControl)
            {
                // keep device awake
                await SensusServiceHelper.KeepDeviceAwakeAsync();

                // increase sampling rate
                if (Protocol.TryGetProbe<ICompassDatum, IListeningProbe>(out IListeningProbe compassProbe))
                {
                    // increase sampling rate
                    compassProbe.MaxDataStoresPerSecond = 20;

                    // restart probe to take on new settings
                    await compassProbe.RestartAsync();
                }
            }
            else if (currentState == SensingAgentState.EndingControl)
            {
                if (Protocol.TryGetProbe<ICompassDatum, IListeningProbe>(out IListeningProbe compassProbe))
                {
                    // decrease sampling rate
                    compassProbe.MaxDataStoresPerSecond = 5;

                    // restart probe to take on original settings
                    await compassProbe.RestartAsync();
                }

                // let device sleep
                await SensusServiceHelper.LetDeviceSleepAsync();
            }
        }
        #endregion

        #region Private Helpers
        /// <summary>
        /// Determines if the observed heading is within the defined range for the sensing agent
        /// </summary>
        /// <param name="heading"> The heading to check for inclusion in target range</param>
        /// <returns>true if in range otherwise false</returns>
        private bool HeadingWithinTargetRange(double heading)
        {
            return ShortestHeadingDistance(TargetHeadingCenter, heading) <= (TargetHeadingRange / 2);
        }

        /// <summary>
        /// Implemented to deal with the "compass problem" (i.e., when degrees cross from 0 to 360)
        /// </summary>
        /// <remarks>
        /// Always returns the an absolute value, representing the distance without direction
        /// </remarks>
        private double ShortestHeadingDistance(double heading1, double heading2)
        {
            var distance = Math.Abs(heading1 - heading2);

            return distance <= 180 ? distance : (360 - distance);
        }
        #endregion
    }
}