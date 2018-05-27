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

namespace Sensus.Tests
{
    [TestFixture]
    public class WindowTests
    {
        [Test]
        public void PointScheduleTriggerParse()
        {
            var t = new Window("10:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.Zero, t.Duration);
        }

        [Test]
        public void WindowScheduleTriggerParse()
        {
            var t = new Window("10:22-12:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.FromHours(2), t.Duration);
        }

        [Test]
        public void PointScheduleToString()
        {
            var t = new Window("10:22");

            Assert.AreEqual("10:22", t.ToString());
        }

        [Test]
        public void WindowScheduleToString()
        {
            var t = new Window("10:22-12:22");

            Assert.AreEqual("10:22-12:22", t.ToString());
        }

        [Test]
        public void WindowScheduleCompareToDifferent()
        {
            var t1 = new Window("10:22-12:22");
            var t2 = new Window("10:23-12:23");

            Assert.True(t1.CompareTo(t2) <= 0);
            Assert.GreaterOrEqual(t2.CompareTo(t1), 0);
        }

        [Test]
        public void WindowScheduleCompareToEqual()
        {
            var t1 = new Window("10:22-12:22");
            var t2 = new Window("10:22-12:22");

            Assert.AreEqual(0, t1.CompareTo(t2));
        }

        [Test]
        public void DowPointScheduleTriggerParse()
        {
            var t = new Window("Su-10:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.Zero, t.Duration);
        }

        [Test]
        public void DowWindowScheduleTriggerParse()
        {
            var t = new Window("Mo-10:22-12:22");

            Assert.AreEqual(new TimeSpan(0, 10, 22, 0), t.Start);
            Assert.AreEqual(new TimeSpan(0, 12, 22, 0), t.End);
            Assert.AreEqual(TimeSpan.FromHours(2), t.Duration);
        }

        [Test]
        public void DowPointScheduleToString()
        {
            var t = new Window("Tu-10:22");

            Assert.AreEqual("Tu-10:22", t.ToString());
        }

        [Test]
        public void DowWindowScheduleToString()
        {
            var t = new Window("We-10:22-12:22");

            Assert.AreEqual("We-10:22-12:22", t.ToString());
        }

        [Test]
        public void DowWindowScheduleCompareToDifferent()
        {
            var t1 = new Window("Th-10:22-12:22");
            var t2 = new Window("Fr-10:22-12:22");

            Assert.True(t1.CompareTo(t2) <= 0);
            Assert.GreaterOrEqual(t2.CompareTo(t1), 0);
        }

        [Test]
        public void DowWindowScheduleCompareToEqual()
        {
            var t1 = new Window("Sa-10:22-12:22");
            var t2 = new Window("Sa-10:22-12:22");

            Assert.AreEqual(0, t1.CompareTo(t2));
        }
    }
}