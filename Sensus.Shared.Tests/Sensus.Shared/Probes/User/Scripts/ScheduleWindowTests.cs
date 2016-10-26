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
using NUnit.Framework;
using Sensus.Probes.User.Scripts;

namespace Sensus.Tests.Scripts
{
    [TestFixture]
    public class ScheduleWindowTests
    {
        [Test]
        public void PointScheduleTriggerParse()
        {
            var t = TriggerWindow.Parse("10:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.Zero, t.Window);
        }

        [Test]
        public void WindowScheduleTriggerParse()
        {
            var t = TriggerWindow.Parse("10:22-12:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.FromHours(2), t.Window);
        }

        [Test]
        public void PointScheduleToString()
        {
            var t = TriggerWindow.Parse("10:22");

            Assert.AreEqual("10:22", t.ToString());
        }

        [Test]
        public void WindowScheduleToString()
        {
            var t = TriggerWindow.Parse("10:22-12:22");

            Assert.AreEqual("10:22-12:22", t.ToString());
        }

        [Test]
        public void NextScheduleWindowNoExpiration()
        {
            var t = TriggerWindow.Parse("10:22-12:22");

            var from  = new DateTime(1986,4,18, 10, 22, 0);
            var after = new DateTime(1986,4,25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(from, after, false, null);

                Assert.GreaterOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(DateTime.MaxValue, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextSchedulePointNoExpiration()
        {
            var t = TriggerWindow.Parse("10:22");

            var from = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(from, after, false, null);

                Assert.AreEqual(TimeSpan.FromDays(8), nextTriggerTime.TimeTill);
                Assert.AreEqual(DateTime.MaxValue, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextSchedulePointExpirationNotTooBig()
        {
            var t = TriggerWindow.Parse("10:22");

            var from = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = from.AddDays(30);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(from, after, false, null);

                Assert.AreEqual(TimeSpan.FromDays(31), nextTriggerTime.TimeTill);
                Assert.AreEqual(DateTime.MaxValue, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextSchedulePointExpirationExpireWindow()
        {
            var t = TriggerWindow.Parse("10:22");

            var from = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = from.AddDays(30);

            var nextTriggerTime = t.GetNextTriggerTime(from, after, true, null);

            Assert.AreEqual(TimeSpan.FromDays(31), nextTriggerTime.TimeTill);
            Assert.AreEqual(DateTime.MaxValue, nextTriggerTime.Expiration);
        }

        [Test]
        public void NextScheduleWindowExpireAge()
        {
            var t = TriggerWindow.Parse("10:22-12:22");

            var from  = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expir = TimeSpan.FromMinutes(10);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(from, after, false, expir);
                
                Assert.GreaterOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(from + nextTriggerTime.TimeTill + expir, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextScheduleWindowWithExpireWindow()
        {
            var t = TriggerWindow.Parse("10:22-12:22");

            var from  = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(from, after, true, null);

                Assert.GreaterOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(from.AddDays(8).AddHours(2), nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextScheduleWindowWithExpirationTime()
        {
            var t = TriggerWindow.Parse("10:22-12:22");

            var from  = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expir = TimeSpan.FromMinutes(1);

            for (var i = 0; i < 100; i++)
            { 
                var nextTriggerTime = t.GetNextTriggerTime(from, after, false, expir);

                Assert.GreaterOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.TimeTill, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.That(nextTriggerTime.Expiration, Is.EqualTo(from + nextTriggerTime.TimeTill + expir).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public void WindowScheduleCompareToDifferent()
        {
            var t1 = TriggerWindow.Parse("10:22-12:22");
            var t2 = TriggerWindow.Parse("10:23-12:23");

            Assert.LessOrEqual(t1.CompareTo(t2), 0);
            Assert.GreaterOrEqual(t2.CompareTo(t1), 0);
        }

        [Test]
        public void WindowScheduleCompareToEqual()
        {
            var t1 = TriggerWindow.Parse("10:22-12:22");
            var t2 = TriggerWindow.Parse("10:22-12:22");

            Assert.AreEqual(0, t1.CompareTo(t2));
        }
    }
}