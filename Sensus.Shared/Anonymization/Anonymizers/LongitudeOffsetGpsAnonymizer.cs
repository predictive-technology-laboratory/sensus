//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
