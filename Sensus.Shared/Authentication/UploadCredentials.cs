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

namespace Sensus.Authentication
{
    public class UploadCredentials
    {
        [JsonProperty(PropertyName = "accessKeyId")]
        public string AccessKeyId { get; set; }

        [JsonProperty(PropertyName = "secretAccessKey")]
        public string SecretAccessKey { get; set; }

        [JsonProperty(PropertyName = "sessionToken")]
        public string SessionToken { get; set; }

        [JsonProperty(PropertyName = "protocolURL")]
        public string ProtocolURL { get; set; }

        [JsonProperty(PropertyName = "protocolId")]
        public string ProtocolId { get; set; }

        [JsonProperty(PropertyName = "expiration")]
        public string Expiration { get; set; }

        [JsonProperty(PropertyName = "cmk")]
        public string CMK { get; set; }

        public DateTimeOffset ExpirationDateTime
        {
            get
            {
                if (long.TryParse(Expiration, out long unixTimeMilliseconds))
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
                }
                else
                {
                    return DateTimeOffset.MinValue;
                }
            }
        }

        public bool WillBeValidFor(TimeSpan? duration)
        {
            return DateTimeOffset.UtcNow + (duration ?? TimeSpan.Zero) < ExpirationDateTime;
        }
    }
}
