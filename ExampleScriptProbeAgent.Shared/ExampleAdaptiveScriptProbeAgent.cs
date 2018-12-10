//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
        private ISensusServiceHelper _sensusServiceHelper;

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
        /// Sets the policy.
        /// </summary>
        /// <param name="policyJSON">Policy json.</param>
        public void SetPolicy(string policyJSON)
        {
            JObject policyObject = JObject.Parse(policyJSON);
            _deliveryProbability = (double)policyObject.GetValue("p");
            _deferralInterval = TimeSpan.FromSeconds((int)policyObject.GetValue("deferral"));

            _sensusServiceHelper?.Logger.Log("Script agent policy set:  p=" + _deliveryProbability + "; deferral=" + _deferralInterval, LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Observe the specified datum.
        /// </summary>
        /// <param name="datum">Datum.</param>
        public void Observe(IDatum datum)
        {
            _sensusServiceHelper?.Logger.Log("Datum observed (" + ++_numDataObserved + " total):  " + datum, LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Checks whether or not to deliver/defer a survey.
        /// </summary>
        /// <returns>Deliver/defer decision.</returns>
        /// <param name="script">Script.</param>
        public async Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNow(IScript script)
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

        /// <summary>
        /// Reset this instance.
        /// </summary>
        /// <param name="sensusServiceHelper">A reference to the Sensus helper.</param>
        public void Reset(ISensusServiceHelper sensusServiceHelper)
        {
            _sensusServiceHelper = sensusServiceHelper;

            _numDataObserved = 0;
            _deliveryProbability = 0.5;

            _sensusServiceHelper?.Logger.Log("Agent has been reset.", LoggingLevel.Normal, GetType());
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
