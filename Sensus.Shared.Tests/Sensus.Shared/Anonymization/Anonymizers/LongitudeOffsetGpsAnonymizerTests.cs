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
using Sensus.Tests.Classes;
using System.Threading.Tasks;

namespace Sensus.Tests.Sensus.Shared.Anonymization.Anonymizers
{
    [TestFixture]
    public class LongitudeOffsetGpsAnonymizerTests
    {
        [Test]
        public async Task LongitudeInRangeNegativeTest()
        {
            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = 30;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(-160.0, protocol);

            Assert.AreEqual(offsetValue, -130, 0.000001);
        }

        [Test]
        public async Task LongitudeInRangePositiveTest()
        {
            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = 20;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(65.0, protocol);

            Assert.AreEqual(offsetValue, 85, 0.000001);
        }

        [Test]
        public async Task LongitudeOutOfRangePositiveTest()
        {
            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = 20;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(170.0, protocol);

            Assert.AreEqual(offsetValue, -170, 0.000001);
        }

        [Test]
        public async Task LongitudeOutOfRangeNegativeTest()
        {
            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = -185;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(-10.0, protocol);

            Assert.AreEqual(offsetValue, 165, 0.000001);
        }

        [Test]
        public async Task RandomParticipantOffsetsEqualTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.ParticipantId = "qwer";

            double randomOffset1 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);
            double randomOffset2 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);

            Assert.AreEqual(randomOffset1, randomOffset2, 0.000001);
        }

        [Test]
        public async Task RandomDeviceIdOffsetsEqualTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.ParticipantId = null;

            double randomOffset1 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);
            double randomOffset2 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);

            Assert.AreEqual(randomOffset1, randomOffset2, 0.000001);
        }
    }
}