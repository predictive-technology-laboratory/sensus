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

using Newtonsoft.Json;

namespace Sensus
{
    public class ProtocolSetting
    {
        /// <summary>
        /// The fully-qualified name of the type whose property (named by <see cref="PropertyName"/>) to be changed. For
        /// example, this would be <c>Sensus.Probes.ListeningProbe</c> if one wishes to change the
        /// <see cref="Probes.ListeningProbe.MaxDataStoresPerSecond"/> property.
        /// </summary>
        /// <value>The name of the property type.</value>
        [JsonProperty("property-type")]
        public string PropertyTypeName { get; set; }

        /// <summary>
        /// The name of the property within the type (named by <see cref="PropertyTypeName"/>) to be changed. For
        /// example, this would be <c>MaxDataStoresPerSecond</c> if one wishes to change the 
        /// <see cref="Probes.ListeningProbe.MaxDataStoresPerSecond"/> property.
        /// </summary>
        /// <value>The name of the property.</value>
        [JsonProperty("property-name")]
        public string PropertyName { get; set; }

        /// <summary>
        /// The fully-qualified name of the type whose instances should have their property values (specifically
        /// the property indicated by <see cref="PropertyTypeName"/> and <see cref="PropertyName"/>) changed. For
        /// example, if one wishes to update <see cref="Probes.ListeningProbe.MaxDataStoresPerSecond"/> for 
        /// all <see cref="Probes.Probe"/>s that inherit from <see cref="Probes.ListeningProbe"/>, then this should
        /// be <c>Sensus.Probes.ListeningProbe</c>. If one wishes to only update 
        /// <see cref="Probes.ListeningProbe.MaxDataStoresPerSecond"/> for only the <see cref="Probes.Movement.AccelerometerProbe"/>, 
        /// then this should be <c>Sensus.Probes.Movement.AccelerometerProbe</c>. Allowing the setting to modify all
        /// inheriting classes provides a convenient mechanism for updating several <see cref="Probes.Probe"/>, 
        /// obviating the need to specify a <see cref="ProtocolSetting"/> for each <see cref="Probes.Probe"/>.
        /// </summary>
        /// <value>The name of the target type.</value>
        [JsonProperty("target-type")]
        public string TargetTypeName { get; set; }

        /// <summary>
        /// Value to set on the property indicated by <see cref="PropertyTypeName"/> and <see cref="PropertyName"/> within
        /// instances of <see cref="TargetTypeName"/>.
        /// </summary>
        /// <value>The value.</value>
        [JsonProperty("value")]
        public object Value { get; set; }
    }
}