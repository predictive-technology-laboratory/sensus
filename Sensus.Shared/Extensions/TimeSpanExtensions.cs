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

namespace Sensus.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string GetFullDescription(this TimeSpan interval, TimeSpan? bufferBefore = null, TimeSpan? bufferAfter = null)
        {
            GetValueAndUnit(interval, out double value, out string unit);

            string description;

            if (Math.Abs(value - 1) < 0.00001)
            {
                description = "Once per " + unit;
            }
            else
            {
                description = "Every " + value + " " + unit + "s";
            }

            // adding the before/after buffer and then showing "+/-" isn't entirely accurate, but it gives the idea.
            TimeSpan totalBuffer = bufferBefore.GetValueOrDefault() + bufferAfter.GetValueOrDefault();
            if (totalBuffer.Ticks != 0)
            {
                description += " (" + totalBuffer.GetPlusMinusDescription() + ")";
            }

            return description;
        }

        public static string GetPlusMinusDescription(this TimeSpan interval)
        {
            GetValueAndUnit(interval, out double value, out string unit);

            string description = "+/- ";

            if (Math.Abs(value - 1) < 0.00001)
            {
                description += "1 " + unit;
            }
            else
            {
                description += value + " " + unit + "s";
            }

            return description;
        }

        private static void GetValueAndUnit(TimeSpan interval, out double value, out string unit)
        {
            value = -1;
            unit = null;
            int decimalPlaces = 0;

            if (interval.TotalSeconds < 60)
            {
                value = interval.TotalSeconds;
                unit = "second";
                decimalPlaces = 1;
            }
            else if (interval.TotalMinutes < 60)
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
        }
    }
}
