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

namespace Sensus.AdaptiveSensing
{
    public class AsplControlCriterion
    {
        [JsonProperty("datum-type")]
        public Type DatumType { get; set; }

        [JsonProperty("datum-property")]
        public string DatumPropertyName { get; set; }

        [JsonProperty("aggregation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AsplControlCriterionAggregation Aggregation { get; set; }

        [JsonProperty("relation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AsplControlCriterionRelation Relation { get; set; }

        [JsonProperty("target")]
        public object Target { get; set; }

        public bool SatisfiedBy(Type type, List<IDatum> data)
        {
            bool satisfied = false;

            if (DatumType == type && data.Count > 0)
            {
                PropertyInfo property = DatumType.GetProperty(DatumPropertyName);

                List<object> values = new List<object>();
                foreach (IDatum datum in data)
                {
                    values.Add(property.GetValue(datum));
                }

                object aggregateValue;

                if (Aggregation == AsplControlCriterionAggregation.Average)
                {
                    aggregateValue = values.Select(value => Convert.ToDouble(value)).Average();
                }
                else if (Aggregation == AsplControlCriterionAggregation.Maximum)
                {
                    aggregateValue = values.Select(value => Convert.ToDouble(value)).Max();
                }
                else if (Aggregation == AsplControlCriterionAggregation.Minimum)
                {
                    aggregateValue = values.Select(value => Convert.ToDouble(value)).Min();
                }
                else if (Aggregation == AsplControlCriterionAggregation.Mode)
                {
                    aggregateValue = values.GroupBy(v => v)
                                           .OrderByDescending(g => g.Count())
                                           .First()
                                           .Key;
                }
                else if (Aggregation == AsplControlCriterionAggregation.Newest)
                {
                    aggregateValue = values.Last();
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (Relation == AsplControlCriterionRelation.EqualTo)
                {
                    satisfied = aggregateValue == Target;
                }
                else if (Relation == AsplControlCriterionRelation.GreaterThan)
                {
                    satisfied = Convert.ToDouble(aggregateValue) > Convert.ToDouble(Target);
                }
                else if (Relation == AsplControlCriterionRelation.GreaterThanOrEqualTo)
                {
                    satisfied = Convert.ToDouble(aggregateValue) >= Convert.ToDouble(Target);
                }
                else if (Relation == AsplControlCriterionRelation.LessThan)
                {
                    satisfied = Convert.ToDouble(aggregateValue) < Convert.ToDouble(Target);
                }
                else if (Relation == AsplControlCriterionRelation.LessThanOrEqualTo)
                {
                    satisfied = Convert.ToDouble(aggregateValue) <= Convert.ToDouble(Target);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return satisfied;
        }
    }
}