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
using Sensus.Probes.Location;
using Sensus.Probes;
using System.Threading;
using Sensus.Extensions;
using Sensus.Adaptation;

namespace ExampleSensingAgent
{
    /// <summary>
    /// Example acceleration sensing agent. Demonstrates concepts related to control criterion checking as well as 
    /// sensing control, in particular temporary device keep-awake and increased sampling rates.
    /// </summary>
    public class ExampleCapstoneAccelerationSensingAgent : SensingAgent
    {
        /// <summary>
        /// Gets or sets the average linear magnitude threshold.
        /// </summary>
        /// <value>The average linear magnitude threshold.</value>
        public double AverageLinearMagnitudeThreshold { get; set; }

        /// <summary>
        /// Gets or sets the control accelerometer max data stores per second.
        /// </summary>
        /// <value>The control accelerometer max data stores per second.</value>
        public double? ControlAccelerometerMaxDataStoresPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the idle accelerometer max data stores per second.
        /// </summary>
        /// <value>The idle accelerometer max data stores per second.</value>
        public double? IdleAccelerometerMaxDataStoresPerSecond { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ExampleSensingAgent.ExampleAccelerationSensingAgent"/> class. As noted in
        /// the [adaptive sensing](xref:adaptive_sensing) article, this class provides the parameterless constructor required for
        /// run time initialization of the agent.
        /// </summary>
        public ExampleCapstoneAccelerationSensingAgent()
            : base("Acceleration", "ALM / Proximity", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        {
            AverageLinearMagnitudeThreshold = 0.1;
            ControlAccelerometerMaxDataStoresPerSecond = 60;
            IdleAccelerometerMaxDataStoresPerSecond = 5;
        }

        protected override Task ProtectedSetPolicyAsync(JObject policy)
        {
            AverageLinearMagnitudeThreshold = double.Parse(policy["alm-threshold"].ToString());
            ControlAccelerometerMaxDataStoresPerSecond = double.Parse(policy["control-acc-rate"].ToString());
            IdleAccelerometerMaxDataStoresPerSecond = double.Parse(policy["idle-acc-rate"].ToString());

            return Task.CompletedTask;
        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData)
        {
            return IsNearSurface() || AverageLinearAccelerationMagnitudeExceedsThreshold(AverageLinearMagnitudeThreshold);
        }

        protected override async Task OnStateChangedAsync(SensingAgentState previousState, SensingAgentState currentState, CancellationToken cancellationToken)
        {
            await base.OnStateChangedAsync(previousState, currentState, cancellationToken);

            if (currentState == SensingAgentState.OpportunisticControl || currentState == SensingAgentState.ActiveControl)
            {
                // keep device awake
                await SensusServiceHelper.KeepDeviceAwakeAsync();

                // increase sampling rate
                if (Protocol.TryGetProbe<IAccelerometerDatum, IListeningProbe>(out IListeningProbe accelerometerProbe))
                {
                    // increase sampling rate
                    accelerometerProbe.MaxDataStoresPerSecond = ControlAccelerometerMaxDataStoresPerSecond;

                    // restart probe to take on new settings
                    await accelerometerProbe.RestartAsync();
                }
            }
            else if (currentState == SensingAgentState.EndingControl)
            {
                if (Protocol.TryGetProbe<IAccelerometerDatum, IListeningProbe>(out IListeningProbe accelerometerProbe))
                {
                    // decrease sampling rate
                    accelerometerProbe.MaxDataStoresPerSecond = IdleAccelerometerMaxDataStoresPerSecond;

                    // restart probe to take on original settings
                    await accelerometerProbe.RestartAsync();
                }

                // let device sleep
                await SensusServiceHelper.LetDeviceSleepAsync();
            }
        }
    }
}