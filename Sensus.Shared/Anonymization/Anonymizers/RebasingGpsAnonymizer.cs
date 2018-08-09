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
using Sensus.Extensions;

namespace Sensus.Anonymization.Anonymizers
{
    /// <summary>
    /// Base class for anonymizers that operate by changing the base (origin)
    /// of the sensed GPS coordinates.
    /// </summary>
    public abstract class RebasingGpsAnonymizer : Anonymizer
    {
        public static Tuple<double, double> GetOrigin(Random random)
        {
            return new Tuple<double, double>(random.NextDouble(-90, 90), random.NextDouble(-180, 180));
        }

        /// <summary>
        /// Gets the origin.
        /// </summary>
        /// <returns>The origin.</returns>
        /// <param name="protocol">Protocol.</param>
        public abstract double GetOrigin(Protocol protocol);
    }
}