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
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sensus.Exceptions;

namespace Sensus.AdaptiveSensing
{
    public class AsplElement
    {
        [JsonProperty("property-type")]
        public string PropertyTypeName { get; set; }

        [JsonProperty("property-name")]
        public string PropertyName { get; set; }

        [JsonProperty("aggregation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AsplAggregation Aggregation { get; set; }

        [JsonProperty("relation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AsplRelation Relation { get; set; }

        [JsonProperty("target")]
        public object Target { get; set; }

        public bool SatisfiedBy(List<IDatum> data)
        {
            bool satisfied = false;

            if (data.Count > 0)
            {
                // get property values to aggregate
                PropertyInfo property = data[0].GetType().GetProperty(PropertyName);
                List<object> propertyValues = new List<object>();
                foreach (IDatum datum in data)
                {
                    propertyValues.Add(property.GetValue(datum));
                }

                // aggregate values
                object aggregateValue;
                if (Aggregation == AsplAggregation.Average)
                {
                    aggregateValue = propertyValues.Select(value => Convert.ToDouble(value)).Average();
                }
                else if (Aggregation == AsplAggregation.Maximum)
                {
                    aggregateValue = propertyValues.Select(value => Convert.ToDouble(value)).Max();
                }
                else if (Aggregation == AsplAggregation.Minimum)
                {
                    aggregateValue = propertyValues.Select(value => Convert.ToDouble(value)).Min();
                }
                else if (Aggregation == AsplAggregation.Mode)
                {
                    aggregateValue = propertyValues.GroupBy(v => v)
                                                   .OrderByDescending(g => g.Count())
                                                   .First()
                                                   .Key;
                }
                else if (Aggregation == AsplAggregation.Newest)
                {
                    aggregateValue = propertyValues.Last();
                }
                else
                {
                    SensusException.Report("Unrecognized aggregation:  " + Aggregation);
                    throw new NotImplementedException();
                }

                // check aggregated value against target per relation
                if (Relation == AsplRelation.EqualTo)
                {
                    satisfied = aggregateValue == Target;
                }
                else if (Relation == AsplRelation.GreaterThan)
                {
                    satisfied = Convert.ToDouble(aggregateValue) > Convert.ToDouble(Target);
                }
                else if (Relation == AsplRelation.GreaterThanOrEqualTo)
                {
                    satisfied = Convert.ToDouble(aggregateValue) >= Convert.ToDouble(Target);
                }
                else if (Relation == AsplRelation.LessThan)
                {
                    satisfied = Convert.ToDouble(aggregateValue) < Convert.ToDouble(Target);
                }
                else if (Relation == AsplRelation.LessThanOrEqualTo)
                {
                    satisfied = Convert.ToDouble(aggregateValue) <= Convert.ToDouble(Target);
                }
                else
                {
                    SensusException.Report("Unrecognized relation:  " + Aggregation);
                    throw new NotImplementedException();
                }
            }

            return satisfied;
        }
    }
}