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

namespace Sensus.AdaptiveSensing
{
    public enum SensingAgentState
    {
        /// <summary>
        /// The <see cref="SensingAgent"/> is neither observing data nor controlling sensing.
        /// </summary>
        Idle,

        /// <summary>
        /// The <see cref="SensingAgent"/> is controlling sensing in response to opportunistic observation.
        /// </summary>
        OpportunisticControl,

        /// <summary>
        /// The <see cref="SensingAgent"/> is actively observing to determine whether <see cref="ActiveControl"/> is desired.
        /// </summary>
        ActiveObservation,

        /// <summary>
        /// The <see cref="SensingAgent"/> is controlling sensing in response to <see cref="ActiveObservation"/>.
        /// </summary>
        ActiveControl,

        /// <summary>
        /// The <see cref="SensingAgent"/> is ending sensing control (either <see cref="OpportunisticControl"/> or <see cref="ActiveControl"/>).
        /// </summary>
        EndingControl
    }
}