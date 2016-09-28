using System;
using NUnit.Framework;
using Sensus.Tools.Scripts;

namespace Sensus.Tools.Tests.Scripts
{
    [TestFixture]
    public class ScheduleTriggerTests
    {
        [Test]
        public void PointScheduleTriggerParse()
        {
            var t = ScheduleTrigger.Parse("10:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.Zero, t.Window);
        }

        [Test]
        public void WindowScheduleTriggerParse()
        {
            var t = ScheduleTrigger.Parse("10:22-12:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.FromHours(2), t.Window);
        }

        [Test]
        public void PointScheduleToString()
        {
            var t = ScheduleTrigger.Parse("10:22");

            Assert.AreEqual("10:22", t.ToString());
        }

        [Test]
        public void WindowScheduleToString()
        {
            var t = ScheduleTrigger.Parse("10:22-12:22");

            Assert.AreEqual("10:22-12:22", t.ToString());
        }

        [Test]
        public void NextScheduleWindowNoExpiration()
        {
            var t = ScheduleTrigger.Parse("10:22-12:22");

            var from  = new DateTime(1986,4,18, 10, 22, 0);
            var after = new DateTime(1986,4,25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextSchedule = t.NextSchedule(from, after, false, null);

                Assert.GreaterOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(DateTime.MaxValue, nextSchedule.ExpireDate);
            }
        }

        [Test]
        public void NextSchedulePointNoExpiration()
        {
            var t = ScheduleTrigger.Parse("10:22");

            var from = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextSchedule = t.NextSchedule(from, after, false, null);

                Assert.AreEqual(TimeSpan.FromDays(8), nextSchedule.TimeUntil);
                Assert.AreEqual(DateTime.MaxValue, nextSchedule.ExpireDate);
            }
        }

        [Test]
        public void NextSchedulePointNoExpirationOverflow()
        {
            var t = ScheduleTrigger.Parse("10:22");

            var from = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = from.AddDays(30);

            for (var i = 0; i < 100; i++)
            {
                var nextSchedule = t.NextSchedule(from, after, false, null);

                Assert.AreEqual(TimeSpan.FromDays(31), nextSchedule.TimeUntil);
                Assert.AreEqual(DateTime.MaxValue, nextSchedule.ExpireDate);
            }
        }

        [Test]
        public void NextScheduleWindowAgeExpiration()
        {
            var t = ScheduleTrigger.Parse("10:22-12:22");

            var from  = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expir = TimeSpan.FromMinutes(10);

            for (var i = 0; i < 100; i++)
            {
                var nextSchedule = t.NextSchedule(from, after, false, expir);
                
                Assert.GreaterOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(from + nextSchedule.TimeUntil + expir, nextSchedule.ExpireDate);
            }
        }

        [Test]
        public void NextScheduleWindowEndExpiration()
        {
            var t = ScheduleTrigger.Parse("10:22-12:22");

            var from = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);

            for (var i = 0; i < 100; i++)
            {
                var nextSchedule = t.NextSchedule(from, after, true, null);

                Assert.GreaterOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(from.AddDays(8).AddHours(2), nextSchedule.ExpireDate);
            }
        }

        [Test]
        public void NextScheduleWindowMinExpiration()
        {
            var t = ScheduleTrigger.Parse("10:22-12:22");

            var from = new DateTime(1986, 4, 18, 10, 22, 0);
            var after = new DateTime(1986, 4, 25, 10, 22, 0);
            var expir = TimeSpan.FromMinutes(1);

            for (var i = 0; i < 100; i++)
            {
                var nextSchedule = t.NextSchedule(from, after, true, expir);

                Assert.GreaterOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8));
                Assert.LessOrEqual(nextSchedule.TimeUntil, TimeSpan.FromDays(8).Add(TimeSpan.FromHours(2)));
                Assert.AreEqual(from + nextSchedule.TimeUntil + expir, nextSchedule.ExpireDate);
            }
        }

        [Test]
        public void WindowScheduleCompareToDifferent()
        {
            var t1 = ScheduleTrigger.Parse("10:22-12:22");
            var t2 = ScheduleTrigger.Parse("10:23-12:23");

            Assert.LessOrEqual(t1.CompareTo(t2), 0);
            Assert.GreaterOrEqual(t2.CompareTo(t1), 0);
        }

        [Test]
        public void WindowScheduleCompareToEqual()
        {
            var t1 = ScheduleTrigger.Parse("10:22-12:22");
            var t2 = ScheduleTrigger.Parse("10:22-12:22");

            Assert.AreEqual(0, t1.CompareTo(t2));
        }
    }
}