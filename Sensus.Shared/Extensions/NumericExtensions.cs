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
    public static class NumericExtensions
    {
        public static int? RoundToWholePercentageOf(this int numerator, int denominator, int wholeNumber)
        {
            if (denominator == 0)
            {
                return null;
            }
            else
            {
                return (100.0 * (numerator / (double)denominator)).RoundToWhole(wholeNumber);
            }
        }

        public static long? RoundToWholePercentageOf(this long numerator, long denominator, int wholeNumber)
        {
            if (denominator == 0)
            {
                return null;
            }
            else
            {
                return (100.0 * (numerator / (double)denominator)).RoundToWhole(wholeNumber);
            }
        }

        public static long? RoundToWholePercentageOf(this double numerator, double denominator, int wholeNumber)
        {
            if (Math.Abs(denominator) < 0.000000d)
            {
                return null;
            }
            else
            {
                return (100.0 * (numerator / (double)denominator)).RoundToWhole(wholeNumber);
            }
        }

        public static int RoundToWhole(this int value, int wholeNumber)
        {
            return wholeNumber * (int)Math.Round(value / (double)wholeNumber);
        }

        public static int RoundToWhole(this double value, int wholeNumber)
        {
            return wholeNumber * (int)Math.Round(value / wholeNumber);
        }
        public static double GetRandom(this (double min, double max) vals, int decimals = 4)
        {
            var location = new Random().NextDouble() * (vals.max - vals.min);
            var rVal = Math.Round(vals.min + location, decimals);
            return rVal;
        }
    }
}