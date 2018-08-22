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

namespace Sensus.Probes.User.Health
{
    public class WheelChairUseDatum : Datum
    {
        private WheelChairUse _wheelChairUse;

        [JsonConverter(typeof(StringEnumConverter))]
        public WheelChairUse WheelchairUse
        {
            get
            {
                return _wheelChairUse;
            }
            set
            {
                _wheelChairUse = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Wheelchair use:  " + _wheelChairUse;
            }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the blood type.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _wheelChairUse;
            }
        }

        public WheelChairUseDatum(DateTimeOffset timestamp, WheelChairUse wheelChairUse)
            : base(timestamp)
        {
            _wheelChairUse = wheelChairUse;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Wheelchair use:  " + _wheelChairUse;
        }
    }
}