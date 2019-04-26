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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Sensus.AdaptiveSensing
{
    /// <summary>
    /// A criterion to check to determine whether sensing control is required.
    /// </summary>
    public class AsplCriterion
    {
        /// <summary>
        /// Gets or sets the combination logic for the <see cref="AsplElement"/>s.
        /// </summary>
        /// <value>The logic.</value>
        [JsonProperty("logic")]
        public AsplLogic Logic { get; set; }

        /// <summary>
        /// The <see cref="AsplElement"/>s to check. If no <see cref="AsplElement"/>s are
        /// provided, then the current <see cref="AsplCriterion"/> will always evaluate to
        /// <c>true</c> if <see cref="Logic"/> is set to <see cref="AsplLogic.Conjunction"/>.
        /// </summary>
        [JsonProperty("elements")]
        public List<AsplElement> Elements;

        /// <summary>
        /// Checks whether the current <see cref="AsplCriterion"/> is satisfied by observed data.
        /// </summary>
        /// <returns><c>true</c>, if by was satisfied, <c>false</c> otherwise.</returns>
        /// <param name="typeData">Observed data.</param>
        public bool SatisfiedBy(Dictionary<Type, List<IDatum>> typeData)
        {
            bool satisfied = false;

            List<bool> elementsSatisfied = new List<bool>();

            foreach (AsplElement element in Elements)
            {
                bool elementSatisfied = false;

                // each element is specific to a particular type of data, as each element will access a particular 
                // property of the data type. only check the element against data for its specified type.
                foreach (Type type in typeData.Keys.Where(type => type.FullName == element.PropertyTypeName))
                {
                    if (element.SatisfiedBy(typeData[type]))
                    {
                        elementSatisfied = true;
                    }
                }

                elementsSatisfied.Add(elementSatisfied);
            }

            if (Logic == AsplLogic.Conjunction)
            {
                // note:  the All predicate will return true for empty sequences
                satisfied = elementsSatisfied.All(value => value);
            }
            else if (Logic == AsplLogic.Disjunction)
            {
                satisfied = elementsSatisfied.Any(value => value);
            }

            return satisfied;
        }
    }
}