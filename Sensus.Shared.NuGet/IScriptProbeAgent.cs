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

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// Interface for all agents that control survey delivery within Sensus.
    /// </summary>
    public interface IScriptProbeAgent
    {
        /// <summary>
        /// Readable description of the agent.
        /// </summary>
        /// <value>The description.</value>
        string Description { get; }

        /// <summary>
        /// Identifier of the agent (unique within the project).
        /// </summary>
        /// <value>The identifier.</value>
        string Id { get; }

        /// <summary>
        /// Sets the policy of the agent
        /// </summary>
        /// <param name="policyJSON">Policy JSON.</param>
        void SetPolicy(string policyJSON);

        /// <summary>
        /// Asks the agent to observe an <see cref="IDatum"/> object that was just generated by Sensus.
        /// </summary>
        /// <param name="datum">Datum.</param>
        void Observe(IDatum datum);

        /// <summary>
        /// Asks the agent whether an <see cref="IScript"/> should be delivered at the present time as scheduled/triggered, or
        /// if the delivery should instead be deferred to a later date.
        /// </summary>
        /// <returns>A tuple indicating (bool) whether the survey should be delivered at the present time, and (DateTimeOffset?) 
        /// if not, what time instead it should be delivered. For example:
        /// 
        ///   * true, null:  Deliver survey now.
        ///   * false, null:  Do not deliver now and never deliver.
        ///   * false, DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5):  Reschedule survey to come back in 5 minutes
        /// </returns>
        /// <param name="script">Script.</param>
        Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNow(IScript script);

        /// <summary>
        /// Asks the agent to observe a new state for an <see cref="IScript"/>.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="state">State.</param>
        void Observe(IScript script, ScriptState state);

        /// <summary>
        /// Asks the agent to reset itself
        /// </summary>
        /// <param name="sensusServiceHelper">A reference to the service helper, which provides access to the app's core functionality.</param>
        void Reset(ISensusServiceHelper sensusServiceHelper);
    }
}
