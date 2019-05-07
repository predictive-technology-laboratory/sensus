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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sensus.Adaptation
{
    /// <summary>
    /// Combination of an <see cref="AsplCriterion"/> and <see cref="ProtocolSetting"/>s to apply when starting and ending sensing control.
    /// </summary>
    public class AsplStatement
    {
        /// <summary>
        /// Short identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Longer description.
        /// </summary>
        /// <value>The description.</value>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Criterion to check.
        /// </summary>
        /// <value>The criterion.</value>
        [JsonProperty("criterion")]
        public AsplCriterion Criterion { get; set; }

        /// <summary>
        /// <see cref="ProtocolSetting"/>s to apply when control begins.
        /// </summary>
        /// <value>The begin control settings.</value>
        [JsonProperty("begin-control-settings")]
        public List<ProtocolSetting> BeginControlSettings { get; set; }

        /// <summary>
        /// <see cref="ProtocolSetting"/>s to apply when control ends.
        /// </summary>
        /// <value>The end control settings.</value>
        [JsonProperty("end-control-settings")]
        public List<ProtocolSetting> EndControlSettings { get; set; }
    }
}
