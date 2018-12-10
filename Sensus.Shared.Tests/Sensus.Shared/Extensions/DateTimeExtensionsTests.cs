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

namespace Sensus.Tests.Extensions
{
    
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void Max()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(2, 3, 4);

            Assert.Equal(date2, date1.Max(date2));
            Assert.Equal(date2, date2.Max(date1));
        }

        [Fact]
        public void MaxEquals()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Max(date2));
            Assert.Equal(date2, date2.Max(date1));
        }

        [Fact]
        public void MaxNullableFirst()
        {
            DateTime? date1 = null;
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Max(date2));
            Assert.Equal(date2, date2.Max(date1));
        }

        [Fact]
        public void MaxNullableSecond()
        {
            var date1 = new DateTime(1, 2, 3);
            DateTime? date2 = null;

            Assert.Equal(date1, date1.Max(date2));
            Assert.Equal(date1, date2.Max(date1));
        }

        [Fact]
        public void Min()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(2, 3, 4);

            Assert.Equal(date1, date1.Min(date2));
            Assert.Equal(date1, date2.Min(date1));
        }

        [Fact]
        public void MinEquals()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Min(date2));
            Assert.Equal(date2, date2.Min(date1));
        }

        [Fact]
        public void MinNullableFirst()
        {
            DateTime? date1 = null;
            var date2 = new DateTime(1, 2, 3);

            Assert.Equal(date2, date1.Min(date2));
            Assert.Equal(date2, date2.Min(date1));
        }

        [Fact]
        public void MinNullableSecond()
        {
            var date1 = new DateTime(1, 2, 3);
            DateTime? date2 = null;

            Assert.Equal(date1, date1.Min(date2));
            Assert.Equal(date1, date2.Min(date1));
        }
    }
}
