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
    /// Example random script probe agent. Will not defer delivery of surveys not selected for delivery.
    /// </summary>
    public class ExampleRandomScriptProbeAgent : IScriptProbeAgent
    {
        private double _deliveryProbability = 0.5;
        private ISensusServiceHelper _sensusServiceHelper;

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
        public async Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNow(IScript script)
        {
            bool deliver = new Random().NextDouble() < _deliveryProbability;

            await (_sensusServiceHelper?.FlashNotificationAsync("Delivery decision:  " + deliver) ?? Task.CompletedTask);

            // do not defer to a future time if survey is not to be delivered
            return new Tuple<bool, DateTimeOffset?>(deliver, null);
        }

        /// <summary>
        /// Observe the specified datum.
        /// </summary>
        /// <param name="datum">Datum.</param>
        public void Observe(IDatum datum)
        {
            _sensusServiceHelper?.Logger.Log("Observed datum:  " + datum, LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Observe the specified script and state.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="state">State.</param>
        public void Observe(IScript script, ScriptState state)
        {
            _sensusServiceHelper?.Logger.Log("Script " + script.IRunner.Name + " state:  " + state, LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Reset this instance.
        /// </summary>
        /// <param name="sensusServiceHelper">Reference to the Sensus helper.</param>
        public void Reset(ISensusServiceHelper sensusServiceHelper)
        {
            _sensusServiceHelper = sensusServiceHelper;
            _sensusServiceHelper?.Logger.Log("Agent has been reset.", LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Sets the policy.
        /// </summary>
        /// <param name="policyJSON">Policy json.</param>
        public void SetPolicy(string policyJSON)
        {
            JObject policyObject = JObject.Parse(policyJSON);
            _deliveryProbability = (double)policyObject.GetValue("p");

            _sensusServiceHelper?.Logger.Log("Script agent policy set:  p=" + _deliveryProbability, LoggingLevel.Normal, GetType());
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
