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
using System.Linq;
using System.Threading;

namespace Sensus.Notifications
{
    /// <summary>
    /// A received push notification.
    /// </summary>
    public class PushNotification
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the protocol identifier.
        /// </summary>
        /// <value>The protocol identifier.</value>
        public string ProtocolId { get; set; }

        /// <summary>
        /// Gets or sets whether Sensus should check for updates.
        /// </summary>
        /// <value>The update.</value>
        public bool Update { get; set; }

        /// <summary>
        /// Gets or sets the backend key for the push notification request.
        /// </summary>
        /// <value>The backend key.</value>
        public Guid? BackendKey { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>The body.</value>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the sound.
        /// </summary>
        /// <value>The sound.</value>
        public string Sound { get; set; }

        public Protocol GetProtocol()
        {
            Protocol protocol = null;

            try
            {
                protocol = SensusServiceHelper.Get().RegisteredProtocols.Single(p => p.Id == ProtocolId);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to get protocol " + ProtocolId + " for push notification:  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            return protocol;
        }
    }
}
