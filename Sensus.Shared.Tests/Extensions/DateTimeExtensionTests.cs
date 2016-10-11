using System;
using NUnit.Framework;
using Sensus.Tools.Extensions;

namespace Sensus.Shared.Tests.Extensions
{
    [TestFixture]
    public class DateTimeExtensionTests
    {
        [Test]
        public void Max()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(2, 3, 4);

            Assert.AreEqual(date2, date1.Max(date2));
            Assert.AreEqual(date2, date2.Max(date1));
        }

        [Test]
        public void MaxEquals()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(1, 2, 3);

            Assert.AreEqual(date2, date1.Max(date2));
            Assert.AreEqual(date2, date2.Max(date1));
        }

        [Test]
        public void MaxNullableFirst()
        {
            DateTime? date1 = null;
            var date2 = new DateTime(1, 2, 3);

            Assert.AreEqual(date2, date1.Max(date2));
            Assert.AreEqual(date2, date2.Max(date1));
        }

        [Test]
        public void MaxNullableSecond()
        {
            var date1       = new DateTime(1, 2, 3);
            DateTime? date2 = null;

            Assert.AreEqual(date1, date1.Max(date2));
            Assert.AreEqual(date1, date2.Max(date1));
        }

        [Test]
        public void Min()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(2, 3, 4);

            Assert.AreEqual(date1, date1.Min(date2));
            Assert.AreEqual(date1, date2.Min(date1));
        }

        [Test]
        public void MinEquals()
        {
            var date1 = new DateTime(1, 2, 3);
            var date2 = new DateTime(1, 2, 3);

            Assert.AreEqual(date2, date1.Min(date2));
            Assert.AreEqual(date2, date2.Min(date1));
        }

        [Test]
        public void MinNullableFirst()
        {
            DateTime? date1 = null;
            var date2 = new DateTime(1, 2, 3);

            Assert.AreEqual(date2, date1.Min(date2));
            Assert.AreEqual(date2, date2.Min(date1));
        }

        [Test]
        public void MinNullableSecond()
        {
            var date1 = new DateTime(1, 2, 3);
            DateTime? date2 = null;

            Assert.AreEqual(date1, date1.Min(date2));
            Assert.AreEqual(date1, date2.Min(date1));
        }
    }
}