﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        /// Interval of time between successive queries regarding the delivery of surveys.
        /// </summary>
        /// <value>The delivery interval.</value>
        TimeSpan? DeliveryInterval { get; }

        /// <summary>
        /// Tolerance for <see cref="DeliveryInterval"/> before the scheduled time, if doing so 
        /// will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        TimeSpan? DeliveryIntervalToleranceBefore { get; }

        /// <summary>
        /// Tolerance for <see cref="DeliveryInterval"/> after the scheduled time, if doing so 
        /// will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        TimeSpan? DeliveryIntervalToleranceAfter { get; }

        /// <summary>
        /// Sets the survey policy of the current <see cref="IScriptProbeAgent"/>. This method will be called in the following
        /// situations:
        /// 
        ///   * When a push notification arrives with a new policy.
        ///   * When the <see cref="IScriptProbeAgent"/> itself instructs the app to update the policy, through a call to
        ///     <see cref="IProtocol.UpdateScriptAgentPolicyAsync(System.Threading.CancellationToken)"/>.
        /// 
        /// In any case, the new policy be passed to this method as a <see cref="JObject"/>.
        /// </summary>
        /// <param name="policy">Policy.</param>
        Task SetPolicyAsync(JObject policy);

        /// <summary>
        /// Asks the agent to observe an <see cref="IDatum"/> object that was just generated by Sensus.
        /// </summary>
        /// <param name="datum">Datum.</param>
        Task ObserveAsync(IDatum datum);

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
        Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNowAsync(IScript script);

        /// <summary>
        /// Asks the agent to observe a new state for an <see cref="IScript"/>.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="state">State.</param>
        Task ObserveAsync(IScript script, ScriptState state);

        /// <summary>
        /// Initializes the <see cref="IScriptProbeAgent"/>. This is called when the <see cref="IProtocol"/> associated with this
        /// <see cref="IScriptProbeAgent"/> is started.
        /// </summary>
        /// <param name="sensusServiceHelper">A reference to the service helper, which provides access to the app's core functionality.</param>
        /// <param name="protocol">A reference to the <see cref="IProtocol"/> associated with this <see cref="IScriptProbeAgent"/>.</param>
        Task InitializeAsync(ISensusServiceHelper sensusServiceHelper, IProtocol protocol);
    }
}