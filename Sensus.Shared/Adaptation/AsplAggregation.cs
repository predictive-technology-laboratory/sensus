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
        /// Maximum age of observed data to aggregate. If <c>null</c>, then the <see cref="AsplAggregationType"/> specified
        /// by <see cref="Type"/> will be applied to all observed data that are currently held in <see cref="SensingAgent"/>'s
        /// cache; otherwise, only observed data whose ages (i.e., time between <see cref="Datum.Timestamp"/> and the current wall 
        /// clock time) are less than <see cref="MaxAge"/> will be aggregated. This property should not be confused with 
        /// <see cref="SensingAgent.MaxObservedDataAge"/>. This property determines the data that should be aggregated for 
        /// <see cref="AsplCriterion"/> checking. These data are a subset of the currently cached data observations. The value
        /// of <see cref="SensingAgent.MaxObservedDataAge"/> determines the size of the observation cache. So, the intent
        /// of <see cref="MaxAge"/> is to improve the sensitivity of the <see cref="AsplCriterion"/>, whereas the intent of
        /// <see cref="SensingAgent.MaxObservedDataAge"/> is to relieve memory pressure. Caution should exercised when setting
        /// these two properties, since an <see cref="AsplCriterion"/> that requires <see cref="MaxAge"/> of 5 minutes would
        /// not operate properly if <see cref="SensingAgent.MaxObservedDataAge"/> were set to 30 seconds.
        /// </summary>
        /// <value>The horizon.</value>
        [JsonProperty("max-age")]
        public TimeSpan? MaxAge { get; set; }

        public override string ToString()
        {
            return Type + (MaxAge == null ? "" : " within " + MaxAge);
        }
    }
}