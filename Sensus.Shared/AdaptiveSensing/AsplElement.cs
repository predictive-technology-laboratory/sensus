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
    /// <summary>
    /// A single check of observed data, aggregated according to <see cref="AsplAggregation"/>, in <see cref="AsplRelation"/> 
    /// to a <see cref="Target"/>.
    /// </summary>
    public class AsplElement
    {
        /// <summary>
        /// Fully-qualified type name containing property to check. For example, if one wishes to check 
        /// <see cref="Probes.Movement.AccelerometerDatum.X"/>, then this should be
        /// <c>Sensus.Probes.Movement.AccelerometerDatum</c>.
        /// </summary>
        /// <value>The name of the property type.</value>
        [JsonProperty("property-type")]
        public string PropertyTypeName { get; set; }

        /// <summary>
        /// Name of property within type (specified by <see cref="PropertyTypeName"/>) whose value should
        /// be checked. For example, if one wishes to check 
        /// <see cref="Probes.Movement.AccelerometerDatum.X"/>, then this should be
        /// <c>X</c>.
        /// </summary>
        /// <value>The name of the property.</value>
        [JsonProperty("property-name")]
        public string PropertyName { get; set; }

        /// <summary>
        /// The observed data will likely contain many values from the property specified by 
        /// <see cref="PropertyTypeName"/> and <see cref="PropertyName"/>. These values need
        /// to be aggregated in some way to check against the <see cref="Target"/> value. This
        /// specifies the <see cref="AsplAggregation"/> to apply to the observed value. For
        /// example, if one wishes to check the average value of 
        /// <see cref="Probes.Movement.AccelerometerDatum.X"/> against the <see cref="Target"/>, 
        /// then this would be <c>Average</c>.
        /// </summary>
        /// <value>The aggregation.</value>
        [JsonProperty("aggregation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AsplAggregation Aggregation { get; set; }

        /// <summary>
        /// Logical <see cref="AsplRelation"/> used to check the aggregate observation (as
        /// specified by <see cref="AsplAggregation"/>) against the <see cref="Target"/>.
        /// For example, if one wishes to check whether the average value of 
        /// <see cref="Probes.Movement.AccelerometerDatum.X"/> is greater than or equal to
        /// the <see cref="Target"/>, then this should be <c>GreaterThanOrEqualTo</c>.
        /// </summary>
        /// <value>The relation.</value>
        [JsonProperty("relation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AsplRelation Relation { get; set; }

        /// <summary>
        /// Target value used to check against the aggregate observation. This can be
        /// a real value (for numeric aggregates), a logical true/false value (for 
        /// logical aggregates), or a double-quoted string value (for nominal aggregates).
        /// </summary>
        /// <value>The target.</value>
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

                // if the aggregate value is numeric, then convert it to a double for uniform
                // equality checking across all numeric types. we might get int, double, etc. 
                // aggregate values, and the type of the aggregate value needs to compare 
                // appropriately with the target. we check for numeric aggregate values by 
                // attempting to parse the aggregate value as a double. the only catch is that
                // string values that happen to be numeric (e.g., "1") should not be converted.
                if (!(aggregateValue is string) && double.TryParse(aggregateValue.ToString(), out double aggregateValueDouble))
                {
                    aggregateValue = aggregateValueDouble;
                }

                // check aggregated value against target per relation
                if (Relation == AsplRelation.EqualTo)
                {
                    // if the aggregate value is an enumerated type, parse the target into that type
                    // so that equality can be checked. we have to do the conversion here because the
                    // target is specified in JSON as a string.
                    if (aggregateValue is Enum)
                    {
                        Enum targetEnum = Enum.Parse(aggregateValue.GetType(), Target.ToString()) as Enum;
                        aggregateValue.Equals(targetEnum);
                    }
                    // if the aggregate value is a double, then convert the target value to a double
                    // and compare in the standard way for doubles. this will ensure that int, float, 
                    // and other numeric target types compare correctly. we are risking overflow 
                    // (e.g., if the target value is an extremely large long), but we don't see a
                    // much risk in this.
                    else if (aggregateValue is double)
                    {
                        double targetDouble = Convert.ToDouble(Target);
                        satisfied = Math.Abs(((double)aggregateValue) - targetDouble) < 0.0000001;
                    }
                    // otherwise, do a standard equality check (e.g., for strings).
                    else
                    {
                        satisfied = aggregateValue.Equals(Target);
                    }
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