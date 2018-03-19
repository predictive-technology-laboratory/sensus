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
using Sensus.Extensions;

namespace Sensus.TestsExtensions
{
    [TestFixture]
    public class NumericExtensionsTests
    {
        [Test]
        public void RoundPercentageTo3Test()
        {
            Assert.AreEqual(64.RoundedPercentageOf(100, 3), 63);
            Assert.AreEqual(65.RoundedPercentageOf(100, 3), 66);
            Assert.AreEqual(0.RoundedPercentageOf(100, 3), 0);
            Assert.AreEqual(99.RoundedPercentageOf(100, 3), 99);
        }

        [Test]
        public void RoundPercentageTo4Test()
        {
            Assert.AreEqual(85.RoundedPercentageOf(100, 4), 84);
            Assert.AreEqual(81.RoundedPercentageOf(100, 4), 80);
            Assert.AreEqual(0.RoundedPercentageOf(100, 4), 0);
            Assert.AreEqual(99.RoundedPercentageOf(100, 4), 100);
        }

        [Test]
        public void RoundPercentageTo5Test()
        {
            Assert.AreEqual(83.RoundedPercentageOf(100, 5), 85);
            Assert.AreEqual(82.RoundedPercentageOf(100, 5), 80);
            Assert.AreEqual(0.RoundedPercentageOf(100, 5), 0);
            Assert.AreEqual(99.RoundedPercentageOf(100, 5), 100);
        }

        [Test]
        public void RoundTo10Test()
        {
            Assert.AreEqual(13.Round(10), 10);
            Assert.AreEqual(99.Round(10), 100);
        }

        [Test]
        public void RoundTo1000Test()
        {
            Assert.AreEqual(1350.Round(1000), 1000);
            Assert.AreEqual(1550.Round(1000), 2000);
        }

        [Test]
        public void ZeroDenominatorTest()
        {
            Assert.AreEqual(Convert.ToString(7.RoundedPercentageOf(7, 1)), "100");
            Assert.AreEqual(Convert.ToString(7.RoundedPercentageOf(0, 1)), "");
        }
    }
}