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
using NUnit.Framework;
using Sensus.Extensions;
using Sensus.Probes.User.Scripts;

namespace Sensus.Tests.Scripts
{
    [TestFixture]
    public class ScheduleTriggerTests
    {
        [Test]
        public void Deserialize1PointTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00" };

            Assert.AreEqual(1, schedule.WindowCount);
            Assert.AreEqual("10:00", schedule.WindowsString);
        }

        [Test]
        public void Deserialize1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00-10:30" };

            Assert.AreEqual(1, schedule.WindowCount);
            Assert.AreEqual("10:00-10:30", schedule.WindowsString);
        }

        [Test]
        public void Deserialize1PointTrailingCommaTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00" };

            Assert.AreEqual(1, schedule.WindowCount);
            Assert.AreEqual("10:00", schedule.WindowsString);
        }

        [Test]
        public void Deserialize1Point1WindowTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,10:10-10:20" };

            Assert.AreEqual(2, schedule.WindowCount);
            Assert.AreEqual("10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Test]
        public void Deserialize1Point1WindowSpacesTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00,                10:10-10:20" };

            Assert.AreEqual(2, schedule.WindowCount);
            Assert.AreEqual("10:00, 10:10-10:20", schedule.WindowsString);
        }

        [Test]
        public void SchedulesAllFutureTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00, 10:10-10:20" };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));
        }

        [Test]
        public void SchedulesOneFutureTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00, 10:20-10:30" };

            var referenceDate = new DateTime(1986, 4, 18, 10, 10, 0);
            var afterDate = new DateTime(1986, 4, 18, 10, 10, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.IsTrue(new TimeSpan(0, 0, 10, 0) <= triggerTimes[0].ReferenceTillTrigger && triggerTimes[0].ReferenceTillTrigger <= new TimeSpan(0, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(0, 23, 50, 0), triggerTimes[1].ReferenceTillTrigger);

            Assert.IsTrue(new TimeSpan(1, 0, 10, 0) <= triggerTimes[2].ReferenceTillTrigger && triggerTimes[2].ReferenceTillTrigger <= new TimeSpan(1, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(1, 23, 50, 0), triggerTimes[3].ReferenceTillTrigger);

            Assert.IsTrue(new TimeSpan(2, 0, 10, 0) <= triggerTimes[4].ReferenceTillTrigger && triggerTimes[4].ReferenceTillTrigger <= new TimeSpan(2, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(2, 23, 50, 0), triggerTimes[5].ReferenceTillTrigger);
        }

        [Test]
        public void SchedulesAfterOneDayTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00, 10:20-10:30" };

            var referenceDate = new DateTime(1986, 4, 18, 10, 10, 0);
            var afterDate = new DateTime(1986, 4, 19, 10, 10, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.IsTrue(new TimeSpan(1, 0, 10, 0) <= triggerTimes[0].ReferenceTillTrigger && triggerTimes[0].ReferenceTillTrigger <= new TimeSpan(1, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(1, 23, 50, 0), triggerTimes[1].ReferenceTillTrigger);

            Assert.IsTrue(new TimeSpan(2, 0, 10, 0) <= triggerTimes[2].ReferenceTillTrigger && triggerTimes[2].ReferenceTillTrigger <= new TimeSpan(2, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(2, 23, 50, 0), triggerTimes[3].ReferenceTillTrigger);

            Assert.IsTrue(new TimeSpan(3, 0, 10, 0) <= triggerTimes[4].ReferenceTillTrigger && triggerTimes[4].ReferenceTillTrigger <= new TimeSpan(3, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(3, 23, 50, 0), triggerTimes[5].ReferenceTillTrigger);
        }

        [Test]
        public void SchedulesPullsOnlySevenDays()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00" };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 19, 0, 0, 0);

            var triggerTimeCount = schedule.GetTriggerTimes(referenceDate, afterDate).Count();

            Assert.AreEqual(7, triggerTimeCount);
        }

        [Test]
        public void SchedulesAllFutureNoExpirationsTest()
        {
            var schedule = new ScheduleTrigger { WindowsString = "10:00, 10:10-10:20" };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(null, triggerTimes[0].Expiration);
            Assert.AreEqual(null, triggerTimes[1].Expiration);
            Assert.AreEqual(null, triggerTimes[2].Expiration);
            Assert.AreEqual(null, triggerTimes[3].Expiration);
            Assert.AreEqual(null, triggerTimes[4].Expiration);
            Assert.AreEqual(null, triggerTimes[5].Expiration);
        }

        [Test]
        public void SchedulesAllFutureExpirationAgeTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowsString = "10:00, 10:10-10:20"
            };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate, TimeSpan.FromMinutes(10)).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(referenceDate + triggerTimes[0].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[0].Expiration);
            Assert.AreEqual(referenceDate + triggerTimes[1].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[1].Expiration);
            Assert.AreEqual(referenceDate + triggerTimes[2].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[2].Expiration);
            Assert.AreEqual(referenceDate + triggerTimes[3].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[3].Expiration);
            Assert.AreEqual(referenceDate + triggerTimes[4].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[4].Expiration);
            Assert.AreEqual(referenceDate + triggerTimes[5].ReferenceTillTrigger + TimeSpan.FromMinutes(10), triggerTimes[5].Expiration);
        }

        [Test]
        public void SchedulesAllFutureExpirationWindowTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowExpiration = true,
                WindowsString = "10:00, 10:10-10:20"
            };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(null, triggerTimes[0].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 18, 10, 20, 00), triggerTimes[1].Expiration);
            Assert.AreEqual(null, triggerTimes[2].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 19, 10, 20, 00), triggerTimes[3].Expiration);
            Assert.AreEqual(null, triggerTimes[4].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 20, 10, 20, 00), triggerTimes[5].Expiration);
        }

        [Test]
        public void SchedulesAllFutureExpirationWindowAndAgeTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowExpiration = true,
                WindowsString = "10:00, 10:10-10:20"
            };

            var referenceDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(referenceDate, afterDate, TimeSpan.FromMinutes(5)).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].ReferenceTillTrigger && triggerTimes[1].ReferenceTillTrigger <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].ReferenceTillTrigger && triggerTimes[3].ReferenceTillTrigger <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].ReferenceTillTrigger);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].ReferenceTillTrigger && triggerTimes[5].ReferenceTillTrigger <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(referenceDate + triggerTimes[0].ReferenceTillTrigger + TimeSpan.FromMinutes(5), triggerTimes[0].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 18, 10, 20, 00).Min(referenceDate + triggerTimes[1].ReferenceTillTrigger + TimeSpan.FromMinutes(5)), triggerTimes[1].Expiration);
            Assert.AreEqual(referenceDate + triggerTimes[2].ReferenceTillTrigger + TimeSpan.FromMinutes(5), triggerTimes[2].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 19, 10, 20, 00).Min(referenceDate + triggerTimes[3].ReferenceTillTrigger + TimeSpan.FromMinutes(5)), triggerTimes[3].Expiration);
            Assert.AreEqual(referenceDate + triggerTimes[4].ReferenceTillTrigger + TimeSpan.FromMinutes(5), triggerTimes[4].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 20, 10, 20, 00).Min(referenceDate + triggerTimes[5].ReferenceTillTrigger + TimeSpan.FromMinutes(5)), triggerTimes[5].Expiration);
        }
    }
}