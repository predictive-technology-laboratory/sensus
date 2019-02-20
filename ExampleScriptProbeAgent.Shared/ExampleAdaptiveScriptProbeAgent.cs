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
using System.Threading;
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
        private ISensusServiceHelper _sensusServiceHelper;
        private IProtocol _protocol;

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description => "p=" + _deliveryProbability + "; deferral=" + _deferralInterval;

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id => "Adaptive";

        /// <summary>
        /// The delivery interval.
        /// </summary>
        public TimeSpan? DeliveryInterval => null;

        /// <summary>
        /// Tolerance before.
        /// </summary>
        public TimeSpan? DeliveryIntervalToleranceBefore => TimeSpan.Zero;

        /// <summary>
        /// Tolerance after.
        /// </summary>
        public TimeSpan? DeliveryIntervalToleranceAfter => TimeSpan.Zero;

        /// <summary>
        /// Sets the policy.
        /// </summary>
        /// <param name="policyJSON">Policy json.</param>
        public Task SetPolicyAsync(string policyJSON)
        {
            JObject policyObject = JObject.Parse(policyJSON);
            _deliveryProbability = (double)policyObject.GetValue("p");
            _deferralInterval = TimeSpan.FromSeconds((int)policyObject.GetValue("deferral"));

            _sensusServiceHelper?.Logger.Log("Script agent policy set:  p=" + _deliveryProbability + "; deferral=" + _deferralInterval, LoggingLevel.Normal, GetType());

            return Task.CompletedTask;
        }

        /// <summary>
        /// Observe the specified datum.
        /// </summary>
        /// <param name="datum">Datum.</param>
        public Task ObserveAsync(IDatum datum)
        {
            _sensusServiceHelper?.Logger.Log("Datum observed (" + ++_numDataObserved + " total):  " + datum, LoggingLevel.Normal, GetType());

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks whether or not to deliver/defer a survey.
        /// </summary>
        /// <returns>Deliver/defer decision.</returns>
        /// <param name="script">Script.</param>
        public async Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNowAsync(IScript script)
        {
            bool deliver = new Random().NextDouble() < _deliveryProbability;

            DateTimeOffset? deferralTime = null;
            if (!deliver)
            {
                deferralTime = DateTimeOffset.UtcNow + _deferralInterval;
            }

            await (_sensusServiceHelper?.FlashNotificationAsync("Delivery decision:  " + deliver) ?? Task.CompletedTask);

            return new Tuple<bool, DateTimeOffset?>(deliver, deferralTime);
        }

        /// <summary>
        /// Observe the specified script and state.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="state">State.</param>
        public Task ObserveAsync(IScript script, ScriptState state)
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

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes this <see cref="IScriptProbeAgent"/>. This is called when the <see cref="IProtocol"/> associated with
        /// this <see cref="IScriptProbeAgent"/> is started.
        /// </summary>
        /// <param name="sensusServiceHelper">A reference to the Sensus helper.</param>
        /// <param name="protocol">A reference to the <see cref="IProtocol"/> associated with this <see cref="IScriptProbeAgent"/>.</param>
        public async Task InitializeAsync(ISensusServiceHelper sensusServiceHelper, IProtocol protocol)
        {
            _sensusServiceHelper = sensusServiceHelper;
            _protocol = protocol;

            // download the initial policy
            try
            {
                await _protocol.UpdateScriptAgentPolicyAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _sensusServiceHelper?.Logger.Log("Exception while downloading the policy:  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            _numDataObserved = 0;
            _deliveryProbability = 0.5;

            _sensusServiceHelper?.Logger.Log("Agent has been initialized.", LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:ExampleScriptProbeAgent.ExampleAdaptiveScriptProbeAgent"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:ExampleScriptProbeAgent.ExampleAdaptiveScriptProbeAgent"/>.</returns>
        public override string ToString()
        {
            return Id + ":  " + Description;
        }
    }
}