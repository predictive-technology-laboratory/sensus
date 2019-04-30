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

namespace Sensus.Adaptation
{
    /// <summary>
    /// A single check of observed data, aggregated according to <see cref="AsplAggregationType"/>, in <see cref="AsplRelation"/> 
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
        /// specifies the <see cref="AsplAggregation"/> to apply to the observed value.
        /// </summary>
        /// <value>The aggregation.</value>
        [JsonProperty("aggregation")]
        public AsplAggregation Aggregation { get; set; }

        /// <summary>
        /// Logical <see cref="AsplRelation"/> used to check the aggregate observation (as
        /// specified by <see cref="AsplAggregationType"/>) against the <see cref="Target"/>.
        /// For example, if one wishes to check whether the average value of 
        /// <see cref="Probes.Movement.AccelerometerDatum.X"/> is greater than or equal to
        /// the <see cref="Target"/>, then this should be <c>GreaterThanOrEqualTo</c>.
        /// </summary>
        /// <value>The relation.</value>
        [JsonProperty("relation")]
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
                PropertyInfo property = data[0].GetType().GetProperty(PropertyName);

                // get the earliest timestamp we will consider during evaluation
                DateTimeOffset? timestampCutoff = null;
                if (Aggregation.Horizon != null)
                {
                    timestampCutoff = DateTimeOffset.UtcNow - Aggregation.Horizon;
                }

                // if we're only looking at the most recent observation, get the most recent 
                // by timestamp and reduce data collection to it alone.
                if (Aggregation.Type == AsplAggregationType.MostRecent)
                {
                    data = new List<IDatum>(new[] { data.OrderByDescending(datum => datum.Timestamp).First() });
                }

                // scan all data
                double? aggregatedValueDouble = null;
                int observationCount = 0;
                foreach (Datum datum in data)
                {
                    // skip any data that come prior to the cutoff. the ordering of data
                    // within the list isn't guaranteed to be sorted by timestamp, so we
                    // can't reliably do any smart termination of this loop.
                    if (timestampCutoff != null && datum.Timestamp < timestampCutoff)
                    {
                        continue;
                    }

                    ++observationCount;

                    object datumValue = property.GetValue(datum);

                    // all numeric values need to be converted to doubles to compare properly in the 
                    // code that follows. do that now.
                    if (datumValue is byte ||
                        datumValue is decimal ||
                        datumValue is float ||
                        datumValue is int ||
                        datumValue is long ||
                        datumValue is sbyte ||
                        datumValue is short ||
                        datumValue is uint ||
                        datumValue is ulong ||
                        datumValue is ushort)
                    {
                        datumValue = Convert.ToDouble(datumValue);
                    }

                    // process aggregation by type
                    if (Aggregation.Type == AsplAggregationType.Any)
                    {
                        satisfied = SatisfiedBy(datumValue);
                    }
                    else if (Aggregation.Type == AsplAggregationType.Average)
                    {
                        if (aggregatedValueDouble == null)
                        {
                            aggregatedValueDouble = (double)datumValue;
                        }
                        else
                        {
                            aggregatedValueDouble += (double)datumValue;
                        }
                    }
                    else if (Aggregation.Type == AsplAggregationType.Maximum)
                    {
                        if (aggregatedValueDouble == null || (double)datumValue > aggregatedValueDouble)
                        {
                            aggregatedValueDouble = (double)datumValue;
                        }
                    }
                    else if (Aggregation.Type == AsplAggregationType.Minimum)
                    {
                        if (aggregatedValueDouble == null || (double)datumValue < aggregatedValueDouble)
                        {
                            aggregatedValueDouble = (double)datumValue;
                        }
                    }
                    else if (Aggregation.Type == AsplAggregationType.MostRecent)
                    {
                        satisfied = SatisfiedBy(datumValue);
                    }

                    if (satisfied)
                    {
                        break;
                    }
                }

                // double values must wait for all data to be scanned before checking
                if (aggregatedValueDouble != null)
                {
                    if (Aggregation.Type == AsplAggregationType.Average)
                    {
                        aggregatedValueDouble = aggregatedValueDouble / observationCount;
                    }

                    satisfied = SatisfiedBy(aggregatedValueDouble.Value);
                }
            }

            return satisfied;
        }

        private bool SatisfiedBy(object value)
        {
            bool satisfied = false;

            if (Relation == AsplRelation.EqualTo)
            {
                // if the aggregate value is an enumerated type, parse the target into that type
                // so that equality can be checked. we have to do the conversion here because the
                // target is specified in JSON as a string.
                if (value is Enum)
                {
                    Enum targetEnum = Enum.Parse(value.GetType(), Target.ToString()) as Enum;
                    satisfied = value.Equals(targetEnum);
                }
                // if the aggregate value is a double, then convert the target value to a double
                // and compare in the standard way for doubles. this will ensure that int, float, 
                // and other numeric target types compare correctly. we are risking overflow 
                // (e.g., if the target value is an extremely large long), but we don't see a
                // much risk in this.
                else if (value is double)
                {
                    satisfied = Math.Abs(((double)value) - Convert.ToDouble(Target)) < 0.0000001;
                }
                // otherwise, do a standard equality check (e.g., for strings).
                else
                {
                    satisfied = value.Equals(Target);
                }
            }
            else if (Relation == AsplRelation.GreaterThan)
            {
                satisfied = Convert.ToDouble(value) > Convert.ToDouble(Target);
            }
            else if (Relation == AsplRelation.GreaterThanOrEqualTo)
            {
                satisfied = Convert.ToDouble(value) >= Convert.ToDouble(Target);
            }
            else if (Relation == AsplRelation.LessThan)
            {
                satisfied = Convert.ToDouble(value) < Convert.ToDouble(Target);
            }
            else if (Relation == AsplRelation.LessThanOrEqualTo)
            {
                satisfied = Convert.ToDouble(value) <= Convert.ToDouble(Target);
            }

            return satisfied;
        }

        public override string ToString()
        {
            return Aggregation + " of " + PropertyTypeName + "." + PropertyName + " " + Relation + " " + Target;
        }
    }
}