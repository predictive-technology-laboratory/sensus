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
    public static class TimeSpanExtensions
    {
        public static string GetIntervalString(this TimeSpan interval)
        {
            double value = -1;
            string unit;
            int decimalPlaces = 0;

            if (interval.TotalSeconds <= 60)
            {
                value = interval.TotalSeconds;
                unit = "second";
                decimalPlaces = 1;
            }
            else if (interval.TotalMinutes <= 60)
            {
                value = interval.TotalMinutes;
                unit = "minute";
            }
            else if (interval.TotalHours <= 24)
            {
                value = interval.TotalHours;
                unit = "hour";
            }
            else
            {
                value = interval.TotalDays;
                unit = "day";
            }

            value = Math.Round(value, decimalPlaces);

            string intervalStr;

            if (Math.Abs(value - 1) < 0.00001)
            {
                intervalStr = "Once per " + unit + ".";
            }
            else
            {
                intervalStr = "Every " + value + " " + unit + "s.";
            }

            return intervalStr;
        }
    }
}
