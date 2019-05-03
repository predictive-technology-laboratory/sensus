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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Sensus.Adaptation
{
    /// <summary>
    /// Represents the state of a <see cref="SensingAgent"/>. See the [adaptive sensing](xref:adaptive_sensing) article for 
    /// more information.
    /// </summary>
    public class SensingAgentStateDatum : Datum
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SensingAgentState PreviousState { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SensingAgentState CurrentState { get; set; }

        public string Description { get; set; }

        public override string DisplayDetail => PreviousState + " --> " + CurrentState;

        public override object StringPlaceholderValue => PreviousState + " --> " + CurrentState;

        public SensingAgentStateDatum(DateTimeOffset timestamp, SensingAgentState previousState, SensingAgentState currentState, string description)
            : base(timestamp)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Description = description;
        }
    }
}