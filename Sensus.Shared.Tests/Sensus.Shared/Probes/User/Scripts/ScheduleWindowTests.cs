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
            var t = new TriggerWindow("10:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.Zero, t.Duration);
        }

        [Test]
        public void WindowScheduleTriggerParse()
        {
            var t = new TriggerWindow("10:22-12:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.FromHours(2), t.Duration);
        }

        [Test]
        public void PointScheduleToString()
        {
            var t = new TriggerWindow("10:22");

            Assert.AreEqual("10:22", t.ToString());
        }

        [Test]
        public void WindowScheduleToString()
        {
            var t = new TriggerWindow("10:22-12:22");

            Assert.AreEqual("10:22-12:22", t.ToString());
        }

        [Test]
        public void NextScheduleWindowNoExpiration()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, null);

                Assert.GreaterOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(null, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextSchedulePointNoExpiration()
        {
            var t = new TriggerWindow("10:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, null);

                Assert.AreEqual(TimeSpan.FromDays(8), nextTriggerTime.ReferenceTillTrigger);
                Assert.AreEqual(null, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextSchedulePointExpirationNotTooBig()
        {
            var t = new TriggerWindow("10:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = reference.AddDays(30);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, null);

                Assert.AreEqual(TimeSpan.FromDays(31), nextTriggerTime.ReferenceTillTrigger);
                Assert.AreEqual(null, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextSchedulePointExpirationExpireWindow()
        {
            var t = new TriggerWindow("10:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = reference.AddDays(30);

            var nextTriggerTime = t.GetNextTriggerTime(reference, after, true, null);

            Assert.AreEqual(TimeSpan.FromDays(31), nextTriggerTime.ReferenceTillTrigger);
            Assert.AreEqual(null, nextTriggerTime.Expiration);
        }

        [Test]
        public void NextScheduleWindowExpireAge()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expire = TimeSpan.FromMinutes(10);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, expire);

                Assert.GreaterOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(reference + nextTriggerTime.ReferenceTillTrigger + expire, nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextScheduleWindowWithExpireWindow()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, true, null);

                Assert.GreaterOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(reference.AddDays(8).AddHours(2), nextTriggerTime.Expiration);
            }
        }

        [Test]
        public void NextScheduleWindowWithExpirationTime()
        {
            var t = new TriggerWindow("10:22-12:22");

            var reference = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expire = TimeSpan.FromMinutes(1);

            for (var i = 0; i < 100; i++)
            {
                var nextTriggerTime = t.GetNextTriggerTime(reference, after, false, expire);

                Assert.GreaterOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextTriggerTime.ReferenceTillTrigger, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.That(nextTriggerTime.Expiration, Is.EqualTo(reference + nextTriggerTime.ReferenceTillTrigger + expire).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public void WindowScheduleCompareToDifferent()
        {
            var t1 = new TriggerWindow("10:22-12:22");
            var t2 = new TriggerWindow("10:23-12:23");

            Assert.LessOrEqual(t1.CompareTo(t2), 0);
            Assert.GreaterOrEqual(t2.CompareTo(t1), 0);
        }

        [Test]
        public void WindowScheduleCompareToEqual()
        {
            var t1 = new TriggerWindow("10:22-12:22");
            var t2 = new TriggerWindow("10:22-12:22");

            Assert.AreEqual(0, t1.CompareTo(t2));
        }
    }
}