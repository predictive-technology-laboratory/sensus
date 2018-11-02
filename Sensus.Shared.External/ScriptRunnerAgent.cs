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

using System.Threading.Tasks;

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// An agent that controls how surveys delivered for a <see cref="ScriptRunner"/> are handled.
    /// </summary>
    public abstract class ScriptRunnerAgent
    {
        /// <summary>
        /// A descriptive name for the agent.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Checks whether a survey should be delivered.
        /// </summary>
        /// <returns>The deliver survey.</returns>
        public abstract Task<bool> ShouldDeliverSurvey();
    }
}