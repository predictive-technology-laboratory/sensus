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

namespace Sensus.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime Max(this DateTime d1, DateTime d2)
        {
            return d1 > d2 ? d1 : d2;
        }

        public static DateTime Max(this DateTime? d1, DateTime d2)
        {
            return d1 > d2 ? d1.Value : d2;
        }

        public static DateTime Max(this DateTime d1, DateTime? d2)
        {
            return d2 > d1 ? d2.Value : d1;
        }

        public static DateTime Min(this DateTime d1, DateTime d2)
        {
            return d1 < d2 ? d1 : d2;
        }

        public static DateTime Min(this DateTime? d1, DateTime d2)
        {
            return d1 < d2 ? d1.Value : d2;
        }

        public static DateTime Min(this DateTime d1, DateTime? d2)
        {
            return d2 < d1 ? d2.Value : d1;
        }

        public static DateTime? Min(this DateTime? d1, DateTime? d2)
        {
            if (d1.HasValue)
            {
                return d1.Value.Min(d2);
            }
            else if (d2.HasValue)
            {
                return d2.Value.Min(d1);
            }
            else
            {
                return default(DateTime?);
            }
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to the Java current time in milliseconds, following the definition provided
        /// [here](https://docs.oracle.com/javase/7/docs/api/java/lang/System.html#currentTimeMillis()), which specifies
        /// that the time is the number of milliseconds elapsed since midnight January 1, 1970.
        /// </summary>
        /// <returns>The java current time millis.</returns>
        /// <param name="dateTime">Date time.</param>
        public static long ToJavaCurrentTimeMillis(this DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan())).TotalMilliseconds;
        }
    }
}
