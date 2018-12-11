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

namespace Sensus.Tests
{
    
    public class WindowTests
    {
        [Fact]
        public void PointScheduleTriggerParse()
        {
            var t = new Window("10:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.Equal(TimeSpan.Zero, t.Duration);
        }

        [Fact]
        public void WindowScheduleTriggerParse()
        {
            var t = new Window("10:22-12:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.Equal(TimeSpan.FromHours(2), t.Duration);
        }

        [Fact]
        public void PointScheduleToString()
        {
            var t = new Window("10:22");

            Assert.Equal("10:22", t.ToString());
        }

        [Fact]
        public void WindowScheduleToString()
        {
            var t = new Window("10:22-12:22");

            Assert.Equal("10:22-12:22", t.ToString());
        }

        [Fact]
        public void WindowScheduleCompareToDifferent()
        {
            var t1 = new Window("10:22-12:22");
            var t2 = new Window("10:23-12:23");

            Assert.True(t1.CompareTo(t2) <= 0);
            Assert.True(t2.CompareTo(t1) >= 0);
        }

        [Fact]
        public void WindowScheduleCompareToEqual()
        {
            var t1 = new Window("10:22-12:22");
            var t2 = new Window("10:22-12:22");

            Assert.Equal(0, t1.CompareTo(t2));
        }

        [Fact]
        public void DowPointScheduleTriggerParse()
        {
            var t = new Window("Su-10:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.Equal(TimeSpan.Zero, t.Duration);
        }

        [Fact]
        public void DowWindowScheduleTriggerParse()
        {
            var t = new Window("Mo-10:22-12:22");

            Assert.Equal(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.Equal(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.Equal(TimeSpan.FromHours(2), t.Duration);
        }

        [Fact]
        public void DowPointScheduleToString()
        {
            var t = new Window("Tu-10:22");

            Assert.Equal("Tu-10:22", t.ToString());
        }

        [Fact]
        public void DowWindowScheduleToString()
        {
            var t = new Window("We-10:22-12:22");

            Assert.Equal("We-10:22-12:22", t.ToString());
        }

        [Fact]
        public void DowWindowScheduleCompareToDifferent()
        {
            var t1 = new Window("Th-10:22-12:22");
            var t2 = new Window("Fr-10:22-12:22");

            Assert.True(t1.CompareTo(t2) <= 0);
            Assert.True(t2.CompareTo(t1) >= 0);
        }

        [Fact]
        public void DowWindowScheduleCompareToEqual()
        {
            var t1 = new Window("Sa-10:22-12:22");
            var t2 = new Window("Sa-10:22-12:22");

            Assert.Equal(0, t1.CompareTo(t2));
        }
    }
}
