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
using Newtonsoft.Json;
using Sensus.Notifications;

namespace Sensus.Callbacks
{
    /// <summary>
    /// Represents a action that should be scheduled with the operating system for execution at a future time.
    /// </summary>
    public class ScheduledCallback
    {
        /// <summary>
        /// Delegate for scheduled callback actions.
        /// </summary>
        /// <param name="id">Identifier of the callback.</param>
        /// <param name="cancellationToken">Cancellation token for action.</param>
        /// <param name="letDeviceSleepCallback">Action to call if the system should be allowed to sleep prior to completion of the action. Can be null.</param>
        /// <returns>A task that can be awaited while the action completes.</returns>
        public delegate Task ActionAsyncDelegate(string id, CancellationToken cancellationToken, Action letDeviceSleepCallback);

        /// <summary>
        /// Action to execute.
        /// </summary>
        /// <value>The action.</value>
        [JsonIgnore]
        public ActionAsyncDelegate ActionAsync { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the invocation identifier.
        /// </summary>
        /// <value>The invocation identifier.</value>
        public string InvocationId { get; set; }

        /// <summary>
        /// Gets or sets the callback's protocol.
        /// </summary>
        /// <value>The protocol identifier.</value>
        public Protocol Protocol { get; set; }

        /// <summary>
        /// Gets or sets the callback timeout. After this time has elapsed, the callback's cancellation token will be cancelled.
        /// </summary>
        /// <value>The callback timeout.</value>
        public TimeSpan? CallbackTimeout { get; set; }

        /// <summary>
        /// Notification message that should be displayed to the user when the callback is invoked.
        /// </summary>
        /// <value>The user notification message.</value>
        public string UserNotificationMessage { get; set; }

        /// <summary>
        /// Source of cancellation tokens to be cancelled when the action times out.
        /// </summary>
        /// <value>The canceller.</value>
        public CancellationTokenSource Canceller { get; set; }

        /// <summary>
        /// UI page to display when callback is returned to app.
        /// </summary>
        /// <value>The display page.</value>
        public DisplayPage DisplayPage { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public ScheduledCallbackState State { get; set; }

        /// <summary>
        /// Gets or sets the delay of the action.
        /// </summary>
        /// <value>The delay.</value>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// For actions that are repeatedly executed, this is the delay between executions.
        /// </summary>
        /// <value>The repeat delay.</value>
        public TimeSpan? RepeatDelay { get; set; }

        /// <summary>
        /// Gets or sets the next execution time for this callback.
        /// </summary>
        /// <value>The next execution time.</value>
        public DateTime? NextExecution { get; set; }

#if __IOS__
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sensus.Callbacks.ScheduledCallback"/> is silent. Silent 
        /// callbacks do not have a message to display to the user via notifications, and the user is never aware of them. These
        /// are only used when Sensus is in the foreground when managing <see cref="ScheduledCallback"/>s. This only applies
        /// to iOS, as there is no need for such silent callbacks in Android where we are free to do things in the background.
        /// </summary>
        /// <value><c>true</c> if silent; otherwise, <c>false</c>.</value>
        public bool Silent { get { return UserNotificationMessage == null; } }
#endif

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private ScheduledCallback()
        {
            Canceller = new CancellationTokenSource();
            DisplayPage = DisplayPage.None;
            State = ScheduledCallbackState.Created;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCallback"/> class.
        /// </summary>
        /// <param name="actionAsync">Action to execute when callback time arrives.</param>
        /// <param name="delay">How long to delay callback execution.</param>
        /// <param name="id">Identifier for callback. Must be unique within the callback domain.</param>
        /// <param name="domain">Domain of callback identifier. All callback IDs within a domain must be unique. If a duplicate ID is provided, it will not be scheduled.</param>
        /// <param name="protocol">Protocol associated with scheduled callback</param>
        /// <param name="callbackTimeout">How long to allow callback to execute before cancelling it.</param>
        /// <param name="userNotificationMessage">Message to display to the user when executing the callback.</param>
        public ScheduledCallback(ActionAsyncDelegate actionAsync,
                                 TimeSpan delay,
                                 string id,
                                 string domain,
                                 Protocol protocol,
                                 TimeSpan? callbackTimeout = null,
                                 string userNotificationMessage = null)
            : this()
        {
            ActionAsync = actionAsync;
            Delay = delay;
            Id = (domain ?? "SENSUS") + "." + id;  // if a domain is not specified, use a global domain.
            Protocol = protocol;
            CallbackTimeout = callbackTimeout;
            UserNotificationMessage = userNotificationMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCallback"/> class.
        /// </summary>
        /// <param name="action">Action to execute when callback time arrives.</param>
        /// <param name="initialDelay">How long to delay callback execution.</param>
        /// <param name="repeatDelay">How long to delay repeating callback executions following the first callback.</param>
        /// <param name="id">Identifier for callback. Must be unique within the callback domain.</param>
        /// <param name="domain">Domain of callback identifier. All callback IDs within a domain must be unique. If a duplicate ID is provided, it will not be scheduled.</param>
        /// <param name="protocol">Protocol associated with scheduled callback</param>
        /// <param name="callbackTimeout">How long to allow callback to execute before cancelling it.</param>
        /// <param name="userNotificationMessage">Message to display to the user when executing the callback.</param>
        public ScheduledCallback(ActionAsyncDelegate action,
                                 TimeSpan initialDelay,
                                 TimeSpan repeatDelay,
                                 string id,
                                 string domain,
                                 Protocol protocol,
                                 TimeSpan? callbackTimeout = null,
                                 string userNotificationMessage = null)
            : this(action,
                   initialDelay,
                   id, 
                   domain, 
                   protocol, 
                   callbackTimeout, 
                   userNotificationMessage)
        {
            RepeatDelay = repeatDelay;
        }
    }
}