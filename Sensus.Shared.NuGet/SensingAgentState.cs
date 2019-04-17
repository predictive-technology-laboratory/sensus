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

namespace Sensus
{
    public enum SensingAgentState
    {
        /// <summary>
        /// The <see cref="SensingAgent"/> is not engaged in either direct observation or an ongoing action.
        /// </summary>
        Idle,

        /// <summary>
        /// The <see cref="SensingAgent"/> is directly observing data.
        /// </summary>
        Observing,

        /// <summary>
        /// On the basis of observed data, the <see cref="SensingAgent"/> is currently executing a sensing control action.
        /// </summary>
        ActionOngoing
    }
}