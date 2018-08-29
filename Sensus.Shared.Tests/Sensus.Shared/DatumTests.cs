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
using Sensus.Anonymization;
using Sensus.Probes.Location;
using Sensus.Probes.Movement;
using Sensus.Tests.Classes;
using System.Threading.Tasks;

namespace Sensus.Tests
{
    [TestFixture]       
    public class DatumTests
    {
        [SetUp]
        public void TestSetUp()
        {
            SensusServiceHelper.ClearSingleton();
        }

        [Test]
        public async Task SerializeDeserializeTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            LocationDatum datum = new LocationDatum(DateTimeOffset.UtcNow, 0.5, 75.5, -35.5);

            Protocol protocol = await Protocol.CreateAsync("test");
            AnonymizedJsonContractResolver anonymizer = new AnonymizedJsonContractResolver(protocol);
            string serializedJSON = datum.GetJSON(anonymizer, false);

            LocationDatum deserializedDatum = Datum.FromJSON(serializedJSON) as LocationDatum;

            Assert.AreEqual(datum.Accuracy, deserializedDatum.Accuracy);
            Assert.AreEqual(datum.BuildId, deserializedDatum.BuildId);
            Assert.AreEqual(datum.DeviceId, deserializedDatum.DeviceId);
            Assert.AreEqual(datum.Id, deserializedDatum.Id);
            Assert.AreEqual(datum.Latitude, deserializedDatum.Latitude);
            Assert.AreEqual(datum.Longitude, deserializedDatum.Longitude);
            Assert.AreEqual(datum.ProtocolId, deserializedDatum.ProtocolId);
            Assert.AreEqual(datum.Timestamp, deserializedDatum.Timestamp);
        }

        [Test]
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

            Assert.AreEqual(datum.BuildId, deserializedDatum.BuildId);
            Assert.AreEqual(datum.DeviceId, deserializedDatum.DeviceId);
            Assert.AreEqual(datum.Id, deserializedDatum.Id);
            Assert.AreEqual(datum.ProtocolId, deserializedDatum.ProtocolId);
            Assert.AreEqual(datum.Timestamp, deserializedDatum.Timestamp);
            Assert.AreEqual(datum.Activity, deserializedDatum.Activity);
            Assert.AreEqual(datum.Confidence, deserializedDatum.Confidence);
            Assert.AreEqual(datum.ActivityStarting, deserializedDatum.ActivityStarting);
            Assert.AreEqual(datum.Phase, deserializedDatum.Phase);
            Assert.AreEqual(datum.State, deserializedDatum.State);
        }
    }
}