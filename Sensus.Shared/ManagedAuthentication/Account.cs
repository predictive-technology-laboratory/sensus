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

using Newtonsoft.Json;

namespace Sensus.ManagedAuthentication
{
    public class Account
    {
        /// <summary>
        /// Gets or sets the participant identifier.
        /// </summary>
        /// <value>The participant identifier.</value>
        [JsonProperty(PropertyName = "participantId")]
        public string ParticipantId { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the protocol URL.
        /// </summary>
        /// <value>The protocol URL.</value>
        [JsonProperty(PropertyName = "protocolURL")]
        public string ProtocolURL { get; set; }

        /// <summary>
        /// Gets or sets the protocol identifier.
        /// </summary>
        /// <value>The protocol identifier.</value>
        [JsonProperty(PropertyName = "protocolId")]
        public string ProtocolId { get; set; }
    }
}