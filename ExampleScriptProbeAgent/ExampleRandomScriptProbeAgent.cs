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

        public string Description => "p=" + _deliveryProbability;

        public string Id => "Random";

        public Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNow(IScript script)
        {
            // do not defer to a future time if survey is not to be delivered
            return Task.FromResult(new Tuple<bool, DateTimeOffset?>(new Random().NextDouble() < _deliveryProbability, null));
        }

        public void Observe(IDatum datum)
        {
            Console.Out.WriteLine("Observed datum:  " + datum);
        }

        public void Observe(IScript script, ScriptState state)
        {
            Console.Out.WriteLine("Script " + script.IRunner.Name + " state:  " + state);
        }

        public void Reset()
        {
            Console.Out.WriteLine("Agent has been reset.");
        }

        public void SetPolicy(string policyJSON)
        {
            JObject policyObject = JObject.Parse(policyJSON);
            _deliveryProbability = (double)policyObject.GetValue("p");
        }

        public override string ToString()
        {
            return Id + ":  " + Description;
        }
    }
}