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
    /// Different ways of combining multiple <see cref="AsplElement"/> values into a single boolean true/false value.
    /// </summary>
    public enum AsplLogic
    {
        /// <summary>
        /// Logical conjunction.
        /// </summary>
        Conjunction,

        /// <summary>
        /// Logical disjunction.
        /// </summary>
        Disjunction
    }
}
