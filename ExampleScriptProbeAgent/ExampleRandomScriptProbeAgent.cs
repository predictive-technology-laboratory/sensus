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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sensus;
using Sensus.Probes.User.Scripts;

namespace ExampleScriptProbeAgent
{
    /// <summary>
    /// Example random script probe agent. Will not defer delivery of surveys not selected for delivery.
    /// </summary>
    public class ExampleRandomScriptProbeAgent : IScriptProbeAgent
    {
        private double _deliveryProbability = 0.5;

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description => "p=" + _deliveryProbability;

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id => "Random";

        /// <summary>
        /// Checks whether or not to deliver/defer a survey.
        /// </summary>
        /// <returns>Deliver/defer decision.</returns>
        /// <param name="script">Script.</param>
        public Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNow(IScript script)
        {
            // do not defer to a future time if survey is not to be delivered
            return Task.FromResult(new Tuple<bool, DateTimeOffset?>(new Random().NextDouble() < _deliveryProbability, null));
        }

        /// <summary>
        /// Observe the specified datum.
        /// </summary>
        /// <param name="datum">Datum.</param>
        public void Observe(IDatum datum)
        {
            Console.Out.WriteLine("Observed datum:  " + datum);
        }

        /// <summary>
        /// Observe the specified script and state.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="state">State.</param>
        public void Observe(IScript script, ScriptState state)
        {
            Console.Out.WriteLine("Script " + script.IRunner.Name + " state:  " + state);
        }

        /// <summary>
        /// Reset this instance.
        /// </summary>
        public void Reset()
        {
            Console.Out.WriteLine("Agent has been reset.");
        }

        /// <summary>
        /// Sets the policy.
        /// </summary>
        /// <param name="policyJSON">Policy json.</param>
        public void SetPolicy(string policyJSON)
        {
            JObject policyObject = JObject.Parse(policyJSON);
            _deliveryProbability = (double)policyObject.GetValue("p");
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:ExampleScriptProbeAgent.ExampleRandomScriptProbeAgent"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:ExampleScriptProbeAgent.ExampleRandomScriptProbeAgent"/>.</returns>
        public override string ToString()
        {
            return Id + ":  " + Description;
        }
    }
}