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
    public class OneTimeCallback : ScheduledCallback
    {
        public OneTimeCallback(ActionDelegate action, string id, string protocolId, string domain, TimeSpan delay, TimeSpan? callbackTimeout = null, string userNotificationMessage = null) : base(action, id, protocolId, domain, callbackTimeout, userNotificationMessage)
        {
            Delay = delay;
        }

        /// <summary>
        /// Gets or sets the delay.
        /// </summary>
        /// <value>
        /// The duration after which this callback should fire.
        /// </value>
        public TimeSpan Delay { get; set; }
    }
}
