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

namespace Sensus.Adaptation
{
    public class AsplAggregation
    {
        /// <summary>
        /// Type of aggregation to apply.
        /// </summary>
        /// <value>The type.</value>
        [JsonProperty("type")]
        public AsplAggregationType Type { get; set; }

        /// <summary>
        /// Time horizon (how far back in the data to go).
        /// </summary>
        /// <value>The horizon.</value>
        [JsonProperty("horizon")]
        public TimeSpan? Horizon { get; set; }

        public override string ToString()
        {
            return Type + (Horizon == null ? "" : " over past " + Horizon);
        }
    }
}