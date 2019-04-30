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
    /// Different ways of comparing the aggregated criterion value (see <see cref="AsplAggregationType"/>) with a target value to determine whether a <see cref="AsplElement"/> was satisfied.
    /// </summary>
    public enum AsplRelation
    {
        /// <summary>
        /// Less than the target.
        /// </summary>
        LessThan,

        /// <summary>
        /// Less than or equal to the target.
        /// </summary>
        LessThanOrEqualTo,

        /// <summary>
        /// Equal to the target. This is the only valid option for qualitative aggregates.
        /// </summary>
        EqualTo,

        /// <summary>
        /// Greater than or equal to the target.
        /// </summary>
        GreaterThanOrEqualTo,

        /// <summary>
        /// Greater than the target.
        /// </summary>
        GreaterThan
    }
}
