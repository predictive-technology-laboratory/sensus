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

using System;
using Xunit;
using Sensus.Anonymization;
using Sensus.Probes.Location;
using Sensus.Probes.Movement;
using Sensus.Tests.Classes;
using System.Threading.Tasks;

namespace Sensus.Tests
{
           
    public class DatumTests
    {
        public DatumTests()
        {
            SensusServiceHelper.ClearSingleton();
        }

        [Fact]
        public async Task SerializeDeserializeTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            LocationDatum datum = new LocationDatum(DateTimeOffset.UtcNow, 0.5, 75.5, -35.5);

            Protocol protocol = await Protocol.CreateAsync("test");
            AnonymizedJsonContractResolver anonymizer = new AnonymizedJsonContractResolver(protocol);
            string serializedJSON = datum.GetJSON(anonymizer, false);

            LocationDatum deserializedDatum = Datum.FromJSON(serializedJSON) as LocationDatum;

            Assert.Equal(datum.Accuracy, deserializedDatum.Accuracy);
            Assert.Equal(datum.BuildId, deserializedDatum.BuildId);
            Assert.Equal(datum.DeviceId, deserializedDatum.DeviceId);
            Assert.Equal(datum.Id, deserializedDatum.Id);
            Assert.Equal(datum.Latitude, deserializedDatum.Latitude);
            Assert.Equal(datum.Longitude, deserializedDatum.Longitude);
            Assert.Equal(datum.ProtocolId, deserializedDatum.ProtocolId);
            Assert.Equal(datum.Timestamp, deserializedDatum.Timestamp);
        }

        [Fact]
        public async Task SerializeDeserializeWithEnumTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);


            ActivityDatum datum = new ActivityDatum(DateTimeOffset.UtcNow,
                                                    Activities.OnBicycle,
                                                    ActivityPhase.Starting,
                                                    ActivityState.Inactive,
                                                    ActivityConfidence.Medium);

            Protocol protocol = await Protocol.CreateAsync("test");
            AnonymizedJsonContractResolver anonymizer = new AnonymizedJsonContractResolver(protocol);
            string serializedJSON = datum.GetJSON(anonymizer, false);

            ActivityDatum deserializedDatum = Datum.FromJSON(serializedJSON) as ActivityDatum;

            Assert.Equal(datum.BuildId, deserializedDatum.BuildId);
            Assert.Equal(datum.DeviceId, deserializedDatum.DeviceId);
            Assert.Equal(datum.Id, deserializedDatum.Id);
            Assert.Equal(datum.ProtocolId, deserializedDatum.ProtocolId);
            Assert.Equal(datum.Timestamp, deserializedDatum.Timestamp);
            Assert.Equal(datum.Activity, deserializedDatum.Activity);
            Assert.Equal(datum.Confidence, deserializedDatum.Confidence);
            Assert.Equal(datum.ActivityStarting, deserializedDatum.ActivityStarting);
            Assert.Equal(datum.Phase, deserializedDatum.Phase);
            Assert.Equal(datum.State, deserializedDatum.State);
        }
    }
}
