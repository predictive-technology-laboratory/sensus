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
using Sensus.Anonymization.Anonymizers;

namespace Sensus.Tests.Sensus.Shared.Anonymization.Anonymizers
{
    [TestFixture]
    public class RebasingGpsAnonymizerTests
    {
        [Test]
        public void LatitudeInRangeNegativeTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(30, 0)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLatitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(-30.0, protocol);

            Assert.True(Math.Abs(rebasedValue - -60) < 0.000001);
        }

        [Test]
        public void LatitudeInRangePositiveTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(20, 0)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLatitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(65.0, protocol);

            Assert.True(Math.Abs(rebasedValue - 45) < 0.000001);
        }

        [Test]
        public void LatitudeOutOfRangePositiveTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(-20, 0)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLatitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(89.0, protocol);

            Assert.True(Math.Abs(rebasedValue - 71) < 0.000001);
        }

        [Test]
        public void LatitudeOutOfRangeNegativeTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(48, 0)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLatitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(-48.0, protocol);

            Assert.True(Math.Abs(rebasedValue - -84) < 0.000001);
        }

        [Test]
        public void LongitudeInRangeNegativeTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(0, -30)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLongitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(-160.0, protocol);

            Assert.True(Math.Abs(rebasedValue - -130) < 0.000001);
        }

        [Test]
        public void LongitudeInRangePositiveTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(0, -20)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLongitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(65.0, protocol);

            Assert.True(Math.Abs(rebasedValue - 85) < 0.000001);
        }

        [Test]
        public void LongitudeOutOfRangePositiveTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(0, -20)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLongitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(170.0, protocol);

            Assert.True(Math.Abs(rebasedValue - -170) < 0.000001);
        }

        [Test]
        public void LongitudeOutOfRangeNegativeTest()
        {
            Protocol protocol = new Protocol("asdf")
            {
                GpsAnonymizationProtocolOrigin = new Tuple<double, double>(0, 185)
            };

            RebasingGpsAnonymizer anonymizer = new RebasingGpsStudyLongitudeAnonymizer();

            double rebasedValue = (double)anonymizer.Apply(-10.0, protocol);

            Assert.True(Math.Abs(rebasedValue - 165) < 0.000001);
        }

        [Test]
        public void RandomOriginTest()
        {
            Assert.True(false);
        }
    }
}
