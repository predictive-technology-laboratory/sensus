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
    public class BloodTypeDatum : Datum
    {
        private BloodType _bloodType;

        [JsonConverter(typeof(StringEnumConverter))]
        public BloodType BloodType
        {
            get
            {
                return _bloodType;
            }
            set
            {
                _bloodType = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Blood type:  " + _bloodType;
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
                return _bloodType;
            }
        }

        public BloodTypeDatum(DateTimeOffset timestamp, BloodType bloodType)
            : base(timestamp)
        {
            _bloodType = bloodType;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "Blood type:  " + _bloodType;
        }
    }
}