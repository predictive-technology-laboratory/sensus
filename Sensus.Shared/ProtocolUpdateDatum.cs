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

namespace Sensus
{
    /// <summary>
    /// Records updates applied to the <see cref="Protocol"/> via [push notification](remote_updates).
    /// </summary>
    public class ProtocolUpdateDatum : Datum
    {
        /// <summary>
        /// The type defining the property named <see cref="PropertyName"/>.
        /// </summary>
        /// <value>The type of the property.</value>
        public string PropertyType { get; set; }

        /// <summary>
        /// The name of the property, defined by <see cref="PropertyType"/>, whose value was set to <see cref="Value"/>.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }

        /// <summary>
        /// The type with a property named <see cref="PropertyName"/>, whose instances were set to <see cref="Value"/>.
        /// </summary>
        /// <value>The type of the target.</value>
        public string TargetType { get; set; }

        /// <summary>
        /// The value that was set on instances of <see cref="TargetType"/>.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }

        public override string DisplayDetail => Value;

        public override object StringPlaceholderValue => Value;

        public ProtocolUpdateDatum(DateTimeOffset timestamp, string propertyType, string propertyName, string targetType, string value)
            : base(timestamp)
        {
            PropertyType = propertyType;
            PropertyName = propertyName;
            TargetType = targetType;
            Value = value;
        }
    }
}