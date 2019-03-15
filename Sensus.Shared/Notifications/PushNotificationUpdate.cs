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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

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
        public string Id { get; set; }

        /// <summary>
        /// Update type.
        /// </summary>
        /// <value>The type.</value>
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PushNotificationUpdateType Type { get; set; }

        /// <summary>
        /// Update content.
        /// </summary>
        /// <value>The content.</value>
        [JsonProperty(PropertyName = "content")]
        public JObject Content { get; set; }

        /// <summary>
        /// Gets the JSON for the current <see cref="PushNotificationUpdate"/>.
        /// </summary>
        /// <value>The JSON.</value>
        [JsonIgnore]
        public JObject JSON
        {
            get
            {
                return JObject.Parse("{" +
                                         "\"type\":" + JsonConvert.ToString(Type) + "," +
                                         "\"content\":" + Content.ToString(Formatting.None) +
                                     "}");
            }
        }
    }
}
