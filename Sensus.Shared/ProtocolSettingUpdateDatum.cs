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
    /// Records updates applied to a <see cref="Protocol"/> (e.g., via [push notification](xref:remote_updates) or [adaptive sensing](xref:adaptive_sensing)).
    /// </summary>
    public class ProtocolSettingUpdateDatum : Datum
    {
        /// <summary>
        /// The type of <see cref="Probes.Probe"/> that was updated.
        /// </summary>
        /// <value>The type of the property.</value>
        public string ProbeType { get; set; }

        /// <summary>
        /// The name of the property, whose value was set to <see cref="Value"/>.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }

        /// <summary>
        /// The value that was set.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the new value string.
        /// </summary>
        /// <value>The new value string.</value>
        public string NewValueString { get; set; }

        public override string DisplayDetail => Value;

        public override object StringPlaceholderValue => Value;

        public ProtocolSettingUpdateDatum(DateTimeOffset timestamp, string probeType, string propertyName, string value, string newValueString)
            : base(timestamp)
        {
            ProbeType = probeType;
            PropertyName = propertyName;
            Value = value;
            NewValueString = newValueString;
        }
    }
}