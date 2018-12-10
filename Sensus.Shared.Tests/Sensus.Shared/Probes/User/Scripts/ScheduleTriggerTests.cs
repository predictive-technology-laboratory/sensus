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
using System.Linq;
using Xunit;
using Sensus.Extensions;
using Sensus.Probes.User.Scripts;

namespace Sensus.Tests.Probes.User.Scripts
{
    
    public class ScheduleTriggerTests
    {
        [Fact]
        public void Deserialize1PointTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("10:00", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00-10:30" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("10:00-10:30", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1PointTrailingCommaTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00," };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("10:00", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1Point1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void Deserialize1Point1WindowSpacesTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,                10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1PointTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "Su-10:00" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("Su-10:00", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "Mo-10:00-10:30" };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("Mo-10:00-10:30", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1PointTrailingCommaTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "Tu-10:00," };

            Assert.Equal(1, schedule.WindowCount);
            Assert.Equal("Tu-10:00", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1Point1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "We-10:00,10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("We-10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void DowDeserialize1Point1WindowSpacesTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,                Th-10:10-10:20" };

            Assert.Equal(2, schedule.WindowCount);
            Assert.Equal("10:00, Th-10:10-10:20", schedule.WindowsString);
        }

        [Fact]
        public void SchedulesPullsOnlyTenDays()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00" };

            var triggerTimeCount = schedule.GetTriggerTimes(DateTime.Now).Count();

            Assert.Equal(10, triggerTimeCount);
        }

        [Fact]
        public void SchedulesAllFutureNoExpirationsTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00, 10:10-10:20" };

            var triggerTimes = schedule.GetTriggerTimes(DateTime.Now).Take(6).ToArray();

            Assert.Equal(null, triggerTimes[0].Expiration);
            Assert.Equal(null, triggerTimes[1].Expiration);
            Assert.Equal(null, triggerTimes[2].Expiration);
            Assert.Equal(null, triggerTimes[3].Expiration);
            Assert.Equal(null, triggerTimes[4].Expiration);
            Assert.Equal(null, triggerTimes[5].Expiration);
        }
    }
}
