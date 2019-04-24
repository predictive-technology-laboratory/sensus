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

using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sensus.Probes;

namespace Sensus.AdaptiveSensing
{
    public class AsplControlSetting
    {
        [JsonProperty("property-type")]
        public string ProbeTypeName { get; set; }

        [JsonProperty("property-name")]
        public string ProbePropertyName { get; set; }

        public string TargetType 
        [JsonProperty("value")]
        public object Value { get; set; }

        public async Task ApplyAsync(Protocol protocol)
        {
            protocol.SetPropertyOnProbes()
            if (protocol.TryGetProbe(ProbeTypeName, out Probe probe))
            {
                PropertyInfo property = probe.GetType().GetProperty(ProbePropertyName);
                property.SetValue(probe, Value);
                await probe.RestartAsync();
            }
        }
    }
}
