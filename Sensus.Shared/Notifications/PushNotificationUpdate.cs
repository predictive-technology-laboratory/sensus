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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Sensus.Notifications
{
    public class PushNotificationUpdate
    {
        /// <summary>
        /// Gets or sets the identifier. If multiple <see cref="PushNotificationUpdate"/>s have the same <see cref="Id"/>, then
        /// the one created most recently will be applied.
        /// </summary>
        /// <value>The identifier.</value>
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Update type.
        /// </summary>
        /// <value>The type.</value>
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PushNotificationUpdateType Type { get; set; }

        /// <summary>
        /// Identifier of <see cref="Protocol"/> to apply update to.
        /// </summary>
        /// <value>The protocol identifier.</value>
        [JsonProperty(PropertyName = "protocol")]
        public string ProtocolId { get; set; }

        /// <summary>
        /// Update content.
        /// </summary>
        /// <value>The content.</value>
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        /// <summary>
        /// Gets the JSON for the current <see cref="PushNotificationUpdate"/>.
        /// </summary>
        /// <value>The JSON.</value>
        [JsonIgnore]
        public string JSON
        {
            get
            {
                return "{" +
                           "\"id\":" + JsonConvert.ToString(Id) + "," +
                           "\"type\":" + JsonConvert.ToString(Type) + "," +
                           "\"protocol\":" + JsonConvert.ToString(ProtocolId) + "," +
                           "\"content\":" + JsonConvert.ToString(Content) +
                       "}";
            }
        }
    }
}
