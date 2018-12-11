//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xunit;
using Sensus.Anonymization.Anonymizers;
using Sensus.Tests.Classes;
using System.Threading.Tasks;

namespace Sensus.Tests.Sensus.Shared.Anonymization.Anonymizers
{
    
    public class LongitudeOffsetGpsAnonymizerTests
    {
        public LongitudeOffsetGpsAnonymizerTests()
        {
            SensusServiceHelper.ClearSingleton();
        }

        [Fact]
        public async Task LongitudeInRangeNegativeTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = 30;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(-160.0, protocol);

            Assert.Equal(offsetValue, -130, 5);
        }

        [Fact]
        public async Task LongitudeInRangePositiveTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = 20;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(65.0, protocol);

            Assert.Equal(offsetValue, 85, 5);
        }

        [Fact]
        public async Task LongitudeOutOfRangePositiveTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = 20;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(170.0, protocol);

            Assert.Equal(offsetValue, -170, 5);
        }

        [Fact]
        public async Task LongitudeOutOfRangeNegativeTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.GpsLongitudeAnonymizationStudyOffset = -185;

            LongitudeStudyOffsetGpsAnonymizer anonymizer = new LongitudeStudyOffsetGpsAnonymizer();

            double offsetValue = (double)anonymizer.Apply(-10.0, protocol);

            Assert.Equal(offsetValue, 165, 5);
        }

        [Fact]
        public async Task RandomParticipantOffsetsEqualTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.ParticipantId = "qwer";

            double randomOffset1 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);
            double randomOffset2 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);

            Assert.Equal(randomOffset1, randomOffset2, 5);
        }

        [Fact]
        public async Task RandomDeviceIdOffsetsEqualTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            Protocol protocol = await Protocol.CreateAsync("asdf");
            protocol.ParticipantId = null;

            double randomOffset1 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);
            double randomOffset2 = LongitudeOffsetGpsAnonymizer.GetOffset(protocol.LongitudeOffsetParticipantSeededRandom);

            Assert.Equal(randomOffset1, randomOffset2, 5);
        }
    }
}
