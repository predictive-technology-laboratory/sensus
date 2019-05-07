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

namespace Sensus.Adaptation
{
    /// <summary>
    /// Different ways of aggregating observed <see cref="IDatum"/> property values into a single value for criterion checking.
    /// The goal is to transform a collection of values into a single value, which can be checked against a target value to
    /// determine whether or not sensing control should be applied. See <see cref="AsplElement"/> for more information.
    /// </summary>
    public enum AsplAggregationType
    {
        /// <summary>
        /// The minimum observed value.
        /// </summary>
        Minimum,

        /// <summary>
        /// The maximum observed value.
        /// </summary>
        Maximum,

        /// <summary>
        /// The average observed value.
        /// </summary>
        Average,

        /// <summary>
        /// Any observed value.
        /// </summary>
        Any,

        /// <summary>
        /// Most recent observed value.
        /// </summary>
        MostRecent
    }
}
