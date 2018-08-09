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

namespace Sensus.Anonymization.Anonymizers
{
    /// <summary>
    /// Base class for anonymizers that operate by changing the base (origin)
    /// of the latitude of a GPS coordinate pair.
    /// </summary>
    public abstract class RebasingGpsLatitudeAnonymizer : RebasingGpsAnonymizer
    {
        private const double MIN_LATITUDE_DEGREE = -90;
        private const double MAX_LATITUDE_DEGREE = 90;

        public override object Apply(object value, Protocol protocol)
        {
            double actualValue = (double)value;

            // get degree distance from actual to origin
            double rebasedValue = actualValue - GetOrigin(protocol);

            // if distance is less than the minimum, add the difference to the minimum.
            if (rebasedValue < MIN_LATITUDE_DEGREE)
            {
                rebasedValue = MIN_LATITUDE_DEGREE + Math.Abs(rebasedValue - MIN_LATITUDE_DEGREE);
            }
            // if distance is greater than the maximum, subtract the difference from the maximum.
            else if (rebasedValue > MAX_LATITUDE_DEGREE)
            {
                rebasedValue = MAX_LATITUDE_DEGREE - Math.Abs(rebasedValue - MAX_LATITUDE_DEGREE);
            }

            return rebasedValue;
        }
    }
}
