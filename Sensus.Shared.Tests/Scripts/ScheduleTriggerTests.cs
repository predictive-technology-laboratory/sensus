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
using Sensus.Shared.Extensions;
using Sensus.Shared.Probes.User.Scripts;

namespace Sensus.Shared.Tests.Scripts
{
    [TestFixture]
    public class ScheduleTriggerTests
    {
        [Test]
        public void Deserialize1PointTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00" };            

            Assert.AreEqual(1, schedule.WindowCount);
            Assert.AreEqual("10:00", schedule.Windows);
        }

        [Test]
        public void Deserialize1WindowTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00-10:30" };

            Assert.AreEqual(1, schedule.WindowCount);
            Assert.AreEqual("10:00-10:30", schedule.Windows);
        }

        [Test]
        public void Deserialize1PointTrailingCommaTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00" };

            Assert.AreEqual(1, schedule.WindowCount);
            Assert.AreEqual("10:00", schedule.Windows);
        }

        [Test]
        public void Deserialize1Point1WindowTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00,10:10-10:20" };

            Assert.AreEqual(2, schedule.WindowCount);
            Assert.AreEqual("10:00, 10:10-10:20", schedule.Windows);
        }

        [Test]
        public void Deserialize1Point1WindowSpacesTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00,                10:10-10:20" };

            Assert.AreEqual(2, schedule.WindowCount);
            Assert.AreEqual("10:00, 10:10-10:20", schedule.Windows);
        }

        [Test]
        public void SchedulesAllFutureTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00, 10:10-10:20" };

            var fromDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(fromDate, afterDate).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].TimeTill);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].TimeTill && triggerTimes[1].TimeTill <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].TimeTill);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].TimeTill && triggerTimes[3].TimeTill <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].TimeTill);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].TimeTill && triggerTimes[5].TimeTill <= new TimeSpan(2, 10, 20, 0));
        }

        [Test]
        public void SchedulesOneFutureTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00, 10:20-10:30" };

            var fromDate = new DateTime(1986, 4, 18, 10, 10, 0);
            var afterDate = new DateTime(1986, 4, 18, 10, 10, 0);

            var triggerTimes = schedule.GetTriggerTimes(fromDate, afterDate).Take(6).ToArray();

            Assert.IsTrue(new TimeSpan(0, 0, 10, 0) <= triggerTimes[0].TimeTill && triggerTimes[0].TimeTill <= new TimeSpan(0, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(0, 23, 50, 0), triggerTimes[1].TimeTill);

            Assert.IsTrue(new TimeSpan(1, 0, 10, 0) <= triggerTimes[2].TimeTill && triggerTimes[2].TimeTill <= new TimeSpan(1, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(1, 23, 50, 0), triggerTimes[3].TimeTill);

            Assert.IsTrue(new TimeSpan(2, 0, 10, 0) <= triggerTimes[4].TimeTill && triggerTimes[4].TimeTill <= new TimeSpan(2, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(2, 23, 50, 0), triggerTimes[5].TimeTill);
        }

        [Test]
        public void SchedulesAfterOneDayTest()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00, 10:20-10:30" };

            var fromDate = new DateTime(1986, 4, 18, 10, 10, 0);
            var afterDate = new DateTime(1986, 4, 19, 10, 10, 0);

            var triggerTimes = schedule.GetTriggerTimes(fromDate, afterDate).Take(6).ToArray();

            Assert.IsTrue(new TimeSpan(1, 0, 10, 0) <= triggerTimes[0].TimeTill && triggerTimes[0].TimeTill <= new TimeSpan(1, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(1, 23, 50, 0), triggerTimes[1].TimeTill);

            Assert.IsTrue(new TimeSpan(2, 0, 10, 0) <= triggerTimes[2].TimeTill && triggerTimes[2].TimeTill <= new TimeSpan(2, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(2, 23, 50, 0), triggerTimes[3].TimeTill);

            Assert.IsTrue(new TimeSpan(3, 0, 10, 0) <= triggerTimes[4].TimeTill && triggerTimes[4].TimeTill <= new TimeSpan(3, 0, 20, 0));
            Assert.AreEqual(new TimeSpan(3, 23, 50, 0), triggerTimes[5].TimeTill);
        }

        [Test]
        public void SchedulesPullsOnlySevenDays()
        {
            var schedule = new ScheduleTrigger { Windows = "10:00" };            

            var fromDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 19, 0, 0, 0);

            var triggerTimeCount = schedule.GetTriggerTimes(fromDate, afterDate).Count();

            Assert.AreEqual(7, triggerTimeCount);
        }

        [Test]
        public void SchedulesAllFutureNoExpirationsTest()
        {
            var schedule = new ScheduleTrigger {Windows = "10:00, 10:10-10:20"};

            var fromDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(fromDate, afterDate).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].TimeTill);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].TimeTill && triggerTimes[1].TimeTill <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].TimeTill);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].TimeTill && triggerTimes[3].TimeTill <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].TimeTill);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].TimeTill && triggerTimes[5].TimeTill <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(DateTime.MaxValue, triggerTimes[0].Expiration);
            Assert.AreEqual(DateTime.MaxValue, triggerTimes[1].Expiration);
            Assert.AreEqual(DateTime.MaxValue, triggerTimes[2].Expiration);
            Assert.AreEqual(DateTime.MaxValue, triggerTimes[3].Expiration);
            Assert.AreEqual(DateTime.MaxValue, triggerTimes[4].Expiration);
            Assert.AreEqual(DateTime.MaxValue, triggerTimes[5].Expiration);
        }

        [Test]
        public void SchedulesAllFutureExpirationAgeTest()
        {
            var schedule = new ScheduleTrigger
            {
                Windows   = "10:00, 10:10-10:20"
            };

            var fromDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(fromDate, afterDate, TimeSpan.FromMinutes(10)).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].TimeTill);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].TimeTill && triggerTimes[1].TimeTill <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].TimeTill);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].TimeTill && triggerTimes[3].TimeTill <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].TimeTill);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].TimeTill && triggerTimes[5].TimeTill <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(fromDate + triggerTimes[0].TimeTill + TimeSpan.FromMinutes(10), triggerTimes[0].Expiration);
            Assert.AreEqual(fromDate + triggerTimes[1].TimeTill + TimeSpan.FromMinutes(10), triggerTimes[1].Expiration);
            Assert.AreEqual(fromDate + triggerTimes[2].TimeTill + TimeSpan.FromMinutes(10), triggerTimes[2].Expiration);
            Assert.AreEqual(fromDate + triggerTimes[3].TimeTill + TimeSpan.FromMinutes(10), triggerTimes[3].Expiration);
            Assert.AreEqual(fromDate + triggerTimes[4].TimeTill + TimeSpan.FromMinutes(10), triggerTimes[4].Expiration);
            Assert.AreEqual(fromDate + triggerTimes[5].TimeTill + TimeSpan.FromMinutes(10), triggerTimes[5].Expiration);
        }

        [Test]
        public void SchedulesAllFutureExpirationWindowTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowExpiration = true,
                Windows      = "10:00, 10:10-10:20"
            };

            var fromDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(fromDate, afterDate).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].TimeTill);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].TimeTill && triggerTimes[1].TimeTill <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].TimeTill);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].TimeTill && triggerTimes[3].TimeTill <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].TimeTill);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].TimeTill && triggerTimes[5].TimeTill <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(DateTime.MaxValue, triggerTimes[0].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 18, 10, 20, 00), triggerTimes[1].Expiration);
            Assert.AreEqual(DateTime.MaxValue, triggerTimes[2].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 19, 10, 20, 00), triggerTimes[3].Expiration);
            Assert.AreEqual(DateTime.MaxValue, triggerTimes[4].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 20, 10, 20, 00), triggerTimes[5].Expiration);
        }

        [Test]
        public void SchedulesAllFutureExpirationWindowAndAgeTest()
        {
            var schedule = new ScheduleTrigger
            {
                WindowExpiration = true,
                Windows = "10:00, 10:10-10:20"
            };

            var fromDate = new DateTime(1986, 4, 18, 0, 0, 0);
            var afterDate = new DateTime(1986, 4, 18, 0, 0, 0);

            var triggerTimes = schedule.GetTriggerTimes(fromDate, afterDate, TimeSpan.FromMinutes(5)).Take(6).ToArray();

            Assert.AreEqual(new TimeSpan(0, 10, 0, 0), triggerTimes[0].TimeTill);
            Assert.IsTrue(new TimeSpan(0, 10, 10, 0) <= triggerTimes[1].TimeTill && triggerTimes[1].TimeTill <= new TimeSpan(0, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(1, 10, 0, 0), triggerTimes[2].TimeTill);
            Assert.IsTrue(new TimeSpan(1, 10, 10, 0) <= triggerTimes[3].TimeTill && triggerTimes[3].TimeTill <= new TimeSpan(1, 10, 20, 0));

            Assert.AreEqual(new TimeSpan(2, 10, 0, 0), triggerTimes[4].TimeTill);
            Assert.IsTrue(new TimeSpan(2, 10, 10, 0) <= triggerTimes[5].TimeTill && triggerTimes[5].TimeTill <= new TimeSpan(2, 10, 20, 0));

            Assert.AreEqual(fromDate + triggerTimes[0].TimeTill + TimeSpan.FromMinutes(5), triggerTimes[0].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 18, 10, 20, 00).Min(fromDate + triggerTimes[1].TimeTill + TimeSpan.FromMinutes(5)), triggerTimes[1].Expiration);
            Assert.AreEqual(fromDate + triggerTimes[2].TimeTill + TimeSpan.FromMinutes(5), triggerTimes[2].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 19, 10, 20, 00).Min(fromDate + triggerTimes[3].TimeTill + TimeSpan.FromMinutes(5)), triggerTimes[3].Expiration);
            Assert.AreEqual(fromDate + triggerTimes[4].TimeTill + TimeSpan.FromMinutes(5), triggerTimes[4].Expiration);
            Assert.AreEqual(new DateTime(1986, 4, 20, 10, 20, 00).Min(fromDate + triggerTimes[5].TimeTill + TimeSpan.FromMinutes(5)), triggerTimes[5].Expiration);
        }
    }
}