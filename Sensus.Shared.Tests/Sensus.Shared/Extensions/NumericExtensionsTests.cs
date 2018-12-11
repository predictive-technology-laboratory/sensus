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
