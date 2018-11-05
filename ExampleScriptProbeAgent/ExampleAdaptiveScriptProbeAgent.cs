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
    /// Example adaptive script probe agent. Increases/decreases probability of survey delivery based on whether
    /// surveys are completed (increase) or deleted (decrease). Will defer delivery of surveys that are not
    /// selected for delivery.
    /// </summary>
    public class ExampleAdaptiveScriptProbeAgent : IScriptProbeAgent
    {
        /// <summary>
        /// How much should the agent be rewarded when the user either opens (positive) or cancels (negative) a survey?
        /// </summary>
        private readonly double OPEN_CANCEL_REWARD = 0.1;

        /// <summary>
        /// How much should the agent be rewarded when the user submits (positive), deletes (negative), or allows to 
        /// expire (negative) the survey?
        /// </summary>
        private readonly double SUBMIT_DELETE_EXPIRE_REWARD = 0.2;

        private long _numDataObserved;
        private double _deliveryProbability = 0.5;
        private TimeSpan _deferralInterval = TimeSpan.FromSeconds(30);

        public string Description => "p=" + _deliveryProbability + "; deferral=" + _deferralInterval;

        public string Id => "Adaptive";

        public void SetPolicy(string policyJSON)
        {
            JObject policyObject = JObject.Parse(policyJSON);
            _deliveryProbability = (double)policyObject.GetValue("p");
            _deferralInterval = TimeSpan.FromSeconds((int)policyObject.GetValue("deferral"));
        }

        public void Observe(IDatum datum)
        {
            Console.Out.WriteLine("Datum observed (" + ++_numDataObserved + " total):  " + datum);
        }

        public Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNow(IScript script)
        {
            bool deliver = new Random().NextDouble() < _deliveryProbability;

            DateTimeOffset? deferralTime = null;
            if (!deliver)
            {
                deferralTime = DateTimeOffset.UtcNow + _deferralInterval;
            }

            return Task.FromResult(new Tuple<bool, DateTimeOffset?>(deliver, deferralTime));
        }

        public void Observe(IScript script, ScriptState state)
        {
            if (state == ScriptState.Opened)
            {
                // max out at p=1
                _deliveryProbability = Math.Min(1, _deliveryProbability + OPEN_CANCEL_REWARD);
            }
            else if (state == ScriptState.Cancelled)
            {
                // never set the probability to 0, as this would discontinue all surveys.
                _deliveryProbability = Math.Max(0.1, _deliveryProbability - OPEN_CANCEL_REWARD);
            }
            else if (state == ScriptState.Submitted)
            {
                // max out at p=1
                _deliveryProbability = Math.Min(1, _deliveryProbability + SUBMIT_DELETE_EXPIRE_REWARD);
            }
            else if (state == ScriptState.Deleted || state == ScriptState.Expired)
            {
                // never set the probability to 0, as this would discontinue all surveys.
                _deliveryProbability = Math.Max(0.1, _deliveryProbability - SUBMIT_DELETE_EXPIRE_REWARD);
            }
        }

        public void Reset()
        {
            _numDataObserved = 0;
            _deliveryProbability = 0.5;
        }

        public override string ToString()
        {
            return Id + ":  " + Description;
        }
    }
}