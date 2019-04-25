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
    public class AsplCriterion
    {
        [JsonProperty("logic")]
        public AsplLogic Logic { get; set; }

        [JsonProperty("elements")]
        public List<AsplElement> Elements;

        public bool SatisfiedBy(Dictionary<Type, List<IDatum>> typeData)
        {
            List<bool> elementsSatisfied = new List<bool>();

            foreach (AsplElement element in Elements)
            {
                bool elementSatisfied = false;

                // each element is specific to a particular type of data, as each element will access a particular 
                // property of the data type. only check the element using data for its specified type.
                foreach (Type type in typeData.Keys.Where(type => type.FullName == element.PropertyTypeName))
                {
                    if (element.SatisfiedBy(typeData[type]))
                    {
                        elementSatisfied = true;
                    }
                }

                elementsSatisfied.Add(elementSatisfied);
            }

            return Logic == AsplLogic.Conjunction ? elementsSatisfied.All(satisfied => satisfied) : elementsSatisfied.Any();
        }
    }
}