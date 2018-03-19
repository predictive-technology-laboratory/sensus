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

namespace Sensus.Callbacks
{
    /// <summary>
    /// Encapsulates information needed to run a scheduled callback.
    /// </summary>
    public class ScheduledCallback
    {
        /// <summary>
        /// Delegate for scheduled callback actions.
        /// </summary>
        /// <param name="name">Name of action.</param>
        /// <param name="cancellationToken">Cancellation token for action.</param>
        /// <param name="letDeviceSleepCallback">Action to call if the system should be allowed to sleep prior to completion of the action. Can be null.</param>
        /// <returns>A task that can be awaited while the action completes.</returns>
        public delegate Task ActionDelegate(string name, CancellationToken cancellationToken, Action letDeviceSleepCallback);

        /// <summary>
        /// Action to invoke.
        /// </summary>
        /// <value>The action.</value>
        public ActionDelegate Action { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the callback's protocol's identifier.
        /// </summary>
        /// <value>The protocol identifier.</value>
        public Protocol Protocol { get; set; }

        /// <summary>
        /// Gets or sets the callback timeout.
        /// </summary>
        /// <value>The callback timeout.</value>
        public TimeSpan? CallbackTimeout { get; set; }

        /// <summary>
        /// Notification message that should be displayed to the user when the callback is invoked.
        /// </summary>
        /// <value>The user notification message.</value>
        public string UserNotificationMessage { get; set; }

        /// <summary>
        /// Source of cancellation tokens when Action is invoked.
        /// </summary>
        /// <value>The canceller.</value>
        public CancellationTokenSource Canceller { get; set; }

        /// <summary>
        /// Page to display when callback is returned to app.
        /// </summary>
        /// <value>The display page.</value>
        public DisplayPage DisplayPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ScheduledCallback"/> is running.
        /// </summary>
        /// <value><c>true</c> if running; otherwise, <c>false</c>.</value>
        public bool Running { get; set; }

        /// <summary>
        /// Gets or sets the delay.
        /// </summary>
        /// <value>The delay.</value>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// Gets or sets the repeat delay for repeating callbacks.
        /// </summary>
        /// <value>The repeat delay.</value>
        public TimeSpan? RepeatDelay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sensus.Callbacks.ScheduledCallback"/> allows lag.
        /// </summary>
        /// <value><c>true</c> if allow lag; otherwise, <c>false</c>.</value>
        public bool? AllowRepeatLag { get; set; }

        /// <summary>
        /// Gets or sets the next execution time for this <see cref="ScheduledCallback"/>.
        /// </summary>
        /// <value>The next execution time.</value>
        public DateTime? NextExecution { get; set; }

#if __IOS__
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sensus.Callbacks.ScheduledCallback"/> is silent. Silent 
        /// callbacks do not have a message to display to the user via notifications, and the user is never aware of them. These
        /// are only used when Sensus is in the foreground when managing <see cref="ScheduledCallback"/>s.
        /// </summary>
        /// <value><c>true</c> if silent; otherwise, <c>false</c>.</value>
        public bool Silent { get { return UserNotificationMessage == null; } }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCallback"/> class.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="delay">Delay</param>
        /// <param name="id">Identifier for callback</param>
        /// <param name="domain">Domain of scheduled callback</param>
        /// <param name="protocol">Protocol of scheduled callback</param>
        /// <param name="callbackTimeout">Callback Timeout</param>
        /// <param name="userNotificationMessage">User notification message</param>
        public ScheduledCallback(ActionDelegate action, 
                                 TimeSpan delay,
                                 string id, 
                                 string domain, 
                                 Protocol protocol, 
                                 TimeSpan? callbackTimeout = null, 
                                 string userNotificationMessage = null)
        {
            Action = action;
            Delay = delay;
            Id = (domain ?? "SENSUS") + "." + id;
            Protocol = protocol;
            CallbackTimeout = callbackTimeout;
            UserNotificationMessage = userNotificationMessage;
            Canceller = new CancellationTokenSource();
            DisplayPage = DisplayPage.None;
            Running = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCallback"/> class.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="initialDelay">Delay</param>
        /// <param name="repeatDelay">Repeat delay</param>
        /// <param name="allowRepeatLag">Whethe or not to allow lag</param>
        /// <param name="id">Identifier for callback</param>
        /// <param name="domain">Domain of scheduled callback</param>
        /// <param name="protocol">Protocol ID of scheduled callback</param>
        /// <param name="callbackTimeout">Callback Timeout</param>
        /// <param name="userNotificationMessage">User notification message</param>
        public ScheduledCallback(ActionDelegate action,
                                 TimeSpan initialDelay,
                                 TimeSpan repeatDelay,
                                 bool allowRepeatLag,
                                 string id,
                                 string domain,
                                 Protocol protocol,
                                 TimeSpan? callbackTimeout = null,
                                 string userNotificationMessage = null)
            : this(action, initialDelay, id, domain, protocol, callbackTimeout, userNotificationMessage)
        {
            RepeatDelay = repeatDelay;
            AllowRepeatLag = allowRepeatLag;
        }
    }
}