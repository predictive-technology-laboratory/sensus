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
using Sensus;
using Sensus.Probes.User.Scripts;

namespace ExampleScriptProbeAgent
{
    public class ExampleScriptProbeAgent : IScriptProbeAgent
    {
        private double DeliveryProbability => 0.5;

        public string Name => "Randon (p = " + DeliveryProbability + ")";

        public string Id => "1";

        public void Observe(IDatum datum)
        {
        }

        public Task<bool> ShouldDeliverSurvey(IScript script)
        {
            return Task.FromResult(new Random().NextDouble() < DeliveryProbability);
        }
    }
}