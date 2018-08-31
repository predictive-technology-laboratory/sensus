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
using Xunit;
using Sensus.Extensions;

namespace Sensus.TestsExtensions
{
    
    public class NumericExtensionsTests
    {
        [Fact]
        public void RoundPercentageTo3Test()
        {
            Assert.Equal(64.RoundToWholePercentageOf(100, 3), 63);
            Assert.Equal(65.RoundToWholePercentageOf(100, 3), 66);
            Assert.Equal(0.RoundToWholePercentageOf(100, 3), 0);
            Assert.Equal(99.RoundToWholePercentageOf(100, 3), 99);
        }

        [Fact]
        public void RoundPercentageTo4Test()
        {
            Assert.Equal(85.RoundToWholePercentageOf(100, 4), 84);
            Assert.Equal(81.RoundToWholePercentageOf(100, 4), 80);
            Assert.Equal(0.RoundToWholePercentageOf(100, 4), 0);
            Assert.Equal(99.RoundToWholePercentageOf(100, 4), 100);
        }

        [Fact]
        public void RoundPercentageTo5Test()
        {
            Assert.Equal(83.RoundToWholePercentageOf(100, 5), 85);
            Assert.Equal(82.RoundToWholePercentageOf(100, 5), 80);
            Assert.Equal(0.RoundToWholePercentageOf(100, 5), 0);
            Assert.Equal(99.RoundToWholePercentageOf(100, 5), 100);
        }

        [Fact]
        public void RoundTo10Test()
        {
            Assert.Equal(13.RoundToWhole(10), 10);
            Assert.Equal(99.RoundToWhole(10), 100);
        }

        [Fact]
        public void RoundTo1000Test()
        {
            Assert.Equal(1350.RoundToWhole(1000), 1000);
            Assert.Equal(1550.RoundToWhole(1000), 2000);
        }

        [Fact]
        public void ZeroDenominatorTest()
        {
            Assert.Equal(Convert.ToString(7.RoundToWholePercentageOf(7, 1)), "100");
            Assert.Equal(Convert.ToString(7.RoundToWholePercentageOf(0, 1)), "");
        }
    }
}