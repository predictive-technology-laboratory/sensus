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
        public static int RoundedPercentageOf(this int numerator, int denominator, int round)
        {
            return (100.0 * (numerator / (double)denominator)).Round(round);
        }

        public static int Round(this int value, int round)
        {
            return round * (int)Math.Round(value / (double)round);
        }

        public static int Round(this double value, int round)
        {
            return round * (int)Math.Round(value / round);
        }
    }
}