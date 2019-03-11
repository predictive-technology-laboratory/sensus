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
        /// <param name="cancellationToken">Cancellation token for action.</param>
        /// <returns>A task that can be awaited while the action completes.</returns>
        public delegate Task ActionAsyncDelegate(CancellationToken cancellationToken);

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
        public TimeSpan? Timeout { get; set; }

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

        /// <summary>
        /// Gets or sets the delay tolerance before <see cref="NextExecution"/>.
        /// </summary>
        /// <value>The delay tolerance.</value>
        public TimeSpan DelayToleranceBefore { get; set; }

        /// <summary>
        /// Gets or sets the delay tolerance after <see cref="NextExecution"/>.
        /// </summary>
        /// <value>The delay tolerance.</value>
        public TimeSpan DelayToleranceAfter { get; set; }

        /// <summary>
        /// Gets the delay tolerance total.
        /// </summary>
        /// <value>The delay tolerance total.</value>
        public TimeSpan DelayToleranceTotal => DelayToleranceBefore + DelayToleranceAfter;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sensus.Callbacks.ScheduledCallback"/> has been batched with another <see cref="ScheduledCallback"/>.
        /// </summary>
        /// <value><c>true</c> if batched; otherwise, <c>false</c>.</value>
        public bool Batched { get; set; }

        /// <summary>
        /// Gets or sets the push notification backend key.
        /// </summary>
        /// <value>The push notification backend key.</value>
        public Guid PushNotificationBackendKey { get; set; }

#if __IOS__
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sensus.Callbacks.ScheduledCallback"/> is silent. Silent 
        /// callbacks do not have a message to display to the user via notifications, and the user is never aware of them. These
        /// are only used when Sensus is in the foreground when managing <see cref="ScheduledCallback"/>s. This only applies
        /// to iOS, as there is no need for such silent callbacks in Android where we are free to do things in the background.
        /// To create a <see cref="ScheduledCallback"/> for which <see cref="Silent"/> is <code>true</code>, pass <code>null</code>
        /// to the constructor for the user notification message.
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
            PushNotificationBackendKey = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCallback"/> class.
        /// </summary>
        /// <param name="actionAsync">Action to execute when callback time arrives.</param>
        /// <param name="delay">How long to delay callback execution.</param>
        /// <param name="id">Identifier for callback. Must be unique within the callback domain.</param>
        /// <param name="domain">Domain of callback identifier. All callback IDs within a domain must be unique. If an ID duplicates another within the same domain, then it will not be scheduled.</param>
        /// <param name="protocol">Protocol associated with scheduled callback</param>
        /// <param name="timeout">How long to allow callback to execute before cancelling it.</param>
        /// <param name="userNotificationMessage">Message to display to the user when executing the callback.</param>
        /// <param name="delayToleranceBefore">Delay tolerance before.</param>
        /// <param name="delayToleranceAfter">Delay tolerance after.</param>
        public ScheduledCallback(ActionAsyncDelegate actionAsync,
                                 TimeSpan delay,
                                 string id,
                                 string domain,
                                 Protocol protocol,
                                 TimeSpan? timeout,
                                 string userNotificationMessage,
                                 TimeSpan delayToleranceBefore,
                                 TimeSpan delayToleranceAfter)
            : this()
        {
            ActionAsync = actionAsync;
            Delay = delay;
            Id = (domain ?? "SENSUS") + "." + id;  // if a domain is not specified, use a global domain.
            Protocol = protocol;
            Timeout = timeout;
            UserNotificationMessage = userNotificationMessage;
            DelayToleranceBefore = delayToleranceBefore;
            DelayToleranceAfter = delayToleranceAfter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCallback"/> class.
        /// </summary>
        /// <param name="actionAsync">Action to execute when callback time arrives.</param>
        /// <param name="initialDelay">How long to delay callback execution.</param>
        /// <param name="repeatDelay">How long to delay repeating callback executions following the first callback.</param>
        /// <param name="id">Identifier for callback. Must be unique within the callback domain.</param>
        /// <param name="domain">Domain of callback identifier. All callback IDs within a domain must be unique. If an ID duplicates another in the same domain, then it will not be scheduled.</param>
        /// <param name="protocol">Protocol associated with scheduled callback</param>
        /// <param name="timeout">How long to allow callback to execute before cancelling it.</param>
        /// <param name="userNotificationMessage">Message to display to the user when executing the callback.</param>
        /// <param name="delayToleranceBefore">Delay tolerance before.</param>
        /// <param name="delayToleranceAfter">Delay tolerance after.</param>
        public ScheduledCallback(ActionAsyncDelegate actionAsync,
                                 TimeSpan initialDelay,
                                 TimeSpan repeatDelay,
                                 string id,
                                 string domain,
                                 Protocol protocol,
                                 TimeSpan? timeout,
                                 string userNotificationMessage,
                                 TimeSpan delayToleranceBefore,
                                 TimeSpan delayToleranceAfter)
            : this(actionAsync,
                   initialDelay,
                   id,
                   domain,
                   protocol,
                   timeout,
                   userNotificationMessage,
                   delayToleranceBefore,
                   delayToleranceAfter)
        {
            RepeatDelay = repeatDelay;
        }
    }
}