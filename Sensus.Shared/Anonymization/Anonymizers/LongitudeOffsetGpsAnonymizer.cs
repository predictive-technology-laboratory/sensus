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
using Sensus.Extensions;

namespace Sensus.Anonymization.Anonymizers
{
    /// <summary>
    /// Base class for anonymizers that operate by adding a random offset
    /// to the longitude of a GPS coordinate pair. Adding an offset to 
    /// latitude is a generally bad idea because the distance between degrees
    /// of longitude vary tremendously across the latitudes.
    /// </summary>
    public abstract class LongitudeOffsetGpsAnonymizer : Anonymizer
    {
        private const double MINIMUM_LONGITUDE_DEGREE = -180;
        private const double MAXIMUM_LONGITUDE_DEGREE = 180;

        public static double GetOffset(Random random)
        {
            return random.NextDouble(MINIMUM_LONGITUDE_DEGREE, MAXIMUM_LONGITUDE_DEGREE);
        }

        protected abstract double GetOffset(Protocol protocol);

        public override object Apply(object value, Protocol protocol)
        {
            double actualValue = (double)value;

            double offsetValue = actualValue + GetOffset(protocol);

            // if offset value is less (more negative) than minimum, subtract the difference from the maximum.
            if (offsetValue < MINIMUM_LONGITUDE_DEGREE)
            {
                offsetValue = MAXIMUM_LONGITUDE_DEGREE - Math.Abs(offsetValue - MINIMUM_LONGITUDE_DEGREE);
            }
            // if offset value is greater than maximum, add the difference to the minimum.
            else if (offsetValue > MAXIMUM_LONGITUDE_DEGREE)
            {
                offsetValue = MINIMUM_LONGITUDE_DEGREE + Math.Abs(offsetValue - MAXIMUM_LONGITUDE_DEGREE);
            }

            return offsetValue;
        }
    }
}