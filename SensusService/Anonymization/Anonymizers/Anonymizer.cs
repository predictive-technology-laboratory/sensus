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

namespace SensusService.Anonymization.Anonymizers
{    
    public abstract class Anonymizer
    {
        [JsonIgnore]
        public abstract string DisplayText { get; }

        /// <summary>
        /// Applies this anonymizer to an object. It is important to always return the same datatype as is passed in.
        /// </summary>
        /// <param name="value">Value.</param>
        public abstract object Apply(object value, Protocol protocol);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            
            // compare most-derived types of current and passed object -- this comparison is used in the UI when the user is selecting the 
            // anonymizer to apply to a datum property.
            return GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

