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