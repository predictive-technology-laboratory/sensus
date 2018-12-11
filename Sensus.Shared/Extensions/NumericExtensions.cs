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

        public static double NextDouble(this Random random, double min, double max)
        {
            return min + random.NextDouble() * (max - min);
        }
    }
}
