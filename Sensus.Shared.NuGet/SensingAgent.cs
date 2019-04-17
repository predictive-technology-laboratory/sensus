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

namespace Sensus
{
    /// <summary>
    /// An agent that observes data collected by the app and takes action to control sensing parameters.
    /// </summary>
    public abstract class SensingAgent
    {
        /// <summary>
        /// Specifies a check to run periodically to (1) assess whether a sensing control action has been completed and (2) to
        /// transition sensing parameters into subsequent settings and thereby conclude the sensing control action.
        /// </summary>
        public class ActionCompletionCheck
        {
            public delegate Task<SensingAgentState> ActionCompletionCheckAsyncDelegate(CancellationToken cancellationToken);

            public ActionCompletionCheckAsyncDelegate ActionAsync { get; set; }
            public TimeSpan CheckInterval { get; set; }
            public string UserNotificationMessage { get; set; }
            public string NotificationUserResponseMessage { get; set; }

            public ActionCompletionCheck(ActionCompletionCheckAsyncDelegate actionAsync, TimeSpan checkInterval, string userNotificationMessage, string notificationUserResponseMessage)
            {
                ActionAsync = actionAsync;
                CheckInterval = checkInterval;
                UserNotificationMessage = userNotificationMessage;
                NotificationUserResponseMessage = notificationUserResponseMessage;
            }
        }

        private ISensusServiceHelper _sensusServiceHelper;
        private IProtocol _protocol;

        /// <summary>
        /// Readable description of the agent.
        /// </summary>
        /// <value>The description.</value>
        public abstract string Description { get; }

        /// <summary>
        /// Identifier of the agent (unique within the project).
        /// </summary>
        /// <value>The identifier.</value>
        public abstract string Id { get; }

        /// <summary>
        /// Interval of time between successive calls to <see cref="ActAsync(CancellationToken)"/>. If <c>null</c>
        /// is returned, then a repeating call to <see cref="ActAsync(CancellationToken)"/> will not be made
        /// and only calls to <see cref="ObserveAsync(IDatum)"/> will provide opportunities for the <see cref="SensingAgent"/>
        /// to control sensing parameters.
        /// </summary>
        /// <value>The action interval.</value>
        public abstract TimeSpan? ActionInterval { get; }

        /// <summary>
        /// Tolerance for <see cref="ActionInterval"/> before the scheduled time, if doing so 
        /// will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        public abstract TimeSpan? ActionIntervalToleranceBefore { get; }

        /// <summary>
        /// Tolerance for <see cref="ActionInterval"/> after the scheduled time, if doing so 
        /// will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        public abstract TimeSpan? ActionIntervalToleranceAfter { get; }

        /// <summary>
        /// Gets the sensus service helper.
        /// </summary>
        /// <value>The sensus service helper.</value>
        protected ISensusServiceHelper SensusServiceHelper => _sensusServiceHelper;

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        /// <value>The protocol.</value>
        protected IProtocol Protocol => _protocol;

        /// <summary>
        /// Initializes the <see cref="SensingAgent"/>. This is called when the <see cref="IProtocol"/> associated with this
        /// <see cref="SensingAgent"/> is started.
        /// </summary>
        /// <param name="sensusServiceHelper">A reference to the service helper, which provides access to the app's core functionality.</param>
        /// <param name="protocol">A reference to the <see cref="IProtocol"/> associated with this <see cref="SensingAgent"/>.</param>
        public virtual async Task InitializeAsync(ISensusServiceHelper sensusServiceHelper, IProtocol protocol)
        {
            _sensusServiceHelper = sensusServiceHelper;
            _protocol = protocol;

            // download the initial policy
            try
            {
                await _protocol.UpdateSensingAgentPolicyAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _sensusServiceHelper?.Logger.Log("Exception while downloading the policy:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        /// <summary>
        /// Sets the control policy of the current <see cref="SensingAgent"/>. This method will be called in the following
        /// situations:
        /// 
        ///   * When a push notification arrives with a new policy.
        ///   * When the <see cref="SensingAgent"/> itself instructs the app to update the policy, through a call to
        ///     <see cref="IProtocol.UpdateSensingAgentPolicyAsync(System.Threading.CancellationToken)"/>.
        /// 
        /// In any case, the new policy will be passed to this method as a <see cref="JObject"/>.
        /// </summary>
        /// <param name="policy">Policy.</param>
        public abstract Task SetPolicyAsync(JObject policy);

        /// <summary>
        /// Instructs the current <see cref="SensingAgent"/> to observe data for a duration of time. Any <see cref="IDatum"/> stored
        /// by the <see cref="IProtocol"/> associated with this <see cref="SensingAgent"/> will be relayed to the 
        /// <see cref="SensingAgent"/> via the <see cref="ObserveAsync(IDatum)"/> method.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="duration">Duration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task ObserveAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            SensusServiceHelper.Logger.Log("Sensing agent " + Id + " is observing data for " + duration + ".", LoggingLevel.Normal, GetType());
            await Task.Delay(duration, cancellationToken);
        }

        /// <summary>
        /// Asks the agent to observe an <see cref="IDatum"/> object that was generated by Sensus.
        /// </summary>
        /// <returns>A <see cref="ActionCompletionCheck"/> to be configured upon return, or <c>null</c> for no such check.</returns>
        /// <param name="datum">Datum.</param>
        public abstract Task<ActionCompletionCheck> ObserveAsync(IDatum datum);

        /// <summary>
        /// Requests that the <see cref="SensingAgent"/> consider taking a sensing control action.
        /// </summary>
        /// <returns>A <see cref="ActionCompletionCheck"/> to be configured upon return, or <c>null</c> for no such check.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        public abstract Task<ActionCompletionCheck> ActAsync(CancellationToken cancellationToken);
    }
}