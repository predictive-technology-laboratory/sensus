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

using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Sensus.Tests.Classes;
using Sensus.Probes.Location;
using Sensus.Probes.User.Scripts;

namespace Sensus.Tests.Core
{
    [TestFixture]
    public class SensusServiceHelperTests
    {
        #region Fields
        private JsonSerializerSettings _jsonSerializerSettings;
        #endregion

        #region SetUp
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _jsonSerializerSettings = SensusServiceHelper.JSON_SERIALIZER_SETTINGS;

            //we don't want to quietly handle errors when testing.
            _jsonSerializerSettings.Error = null;
        }

        [SetUp]
        public void TestSetUp()
        {
            SensusServiceHelper.ClearSingleton();
        }
        #endregion

        [Test]
        public void SerializeAndDeserializeNoExceptionTest()
        {
            var service1 = new TestSensusServiceHelper();

            var serial1 = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);
            service1.RegisteredProtocols.Clear();
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial1, _jsonSerializerSettings);

            Assert.AreEqual(0, service1.RegisteredProtocols.Count);
            Assert.AreEqual(0, service2.RegisteredProtocols.Count);
        }

        [Test]
        public void RegisteredOneProtocolTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1, false);

            service1.RegisteredProtocols.Clear();

            Protocol.Create("Test");

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.AreEqual(service1.RegisteredProtocols.Count, service2.RegisteredProtocols.Count);
            Assert.AreEqual(service1.RegisteredProtocols.First().Name, service2.RegisteredProtocols.First().Name);
        }

        [Test]
        public void RegisteredTwoProtocolsTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1, false);

            service1.RegisteredProtocols.Clear();

            Protocol.Create("Test1");
            Protocol.Create("Test2");

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);
            SensusServiceHelper.ClearSingleton();
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.AreEqual(2, service1.RegisteredProtocols.Count);
            Assert.AreEqual(2, service2.RegisteredProtocols.Count);

            Assert.AreEqual(service1.RegisteredProtocols.Skip(0).Take(1).Single().Name, service2.RegisteredProtocols.Skip(0).Take(1).Single().Name);
            Assert.AreEqual(service1.RegisteredProtocols.Skip(1).Take(1).Single().Name, service2.RegisteredProtocols.Skip(1).Take(1).Single().Name);
        }

        [Test]
        public void RunningProtocolIdsTest()
        {
            var service1 = new TestSensusServiceHelper();

            SensusServiceHelper.Initialize(() => service1, false);

            Protocol.Create("Test");

            service1.RunningProtocolIds.Clear();
            service1.RunningProtocolIds.Add(service1.RegisteredProtocols.Single().Id);

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.AreEqual(service1.RunningProtocolIds.Count, service2.RunningProtocolIds.Count);
            Assert.AreEqual(service1.RunningProtocolIds.Single(), service2.RunningProtocolIds.Single());
        }

        [Test]
        public void PointsOfInterestTest()
        {
            var service1 = new TestSensusServiceHelper();

            SensusServiceHelper.Initialize(() => service1, false);

            service1.PointsOfInterest.Clear();
            service1.PointsOfInterest.Add(new PointOfInterest("Test", "Test", null));

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.AreEqual(service1.PointsOfInterest.Count, service2.PointsOfInterest.Count);
            Assert.AreEqual(service1.PointsOfInterest.Single().Name, service2.PointsOfInterest.Single().Name);
        }

        [Test]
        public void FlashNotificationsEnabledTest()
        {
            var service1 = new TestSensusServiceHelper();

            SensusServiceHelper.Initialize(() => service1, false);

            service1.FlashNotificationsEnabled = true;

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service1.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);

            service1.FlashNotificationsEnabled = false;

            serial = JsonConvert.SerializeObject(service1);

            SensusServiceHelper.ClearSingleton();

            service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service1.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);
        }

        [Test]
        public void ScriptsToRunTest()
        {
            var service1 = new TestSensusServiceHelper();

            SensusServiceHelper.Initialize(() => service1, false);

            service1.RunningProtocolIds.Clear();

            service1.ScriptsToRun.Add(new Script(new ScriptRunner("Test", new ScriptProbe())));

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service1.ScriptsToRun.Count, service2.ScriptsToRun.Count);
            Assert.AreEqual(service1.ScriptsToRun.Single().Id, service2.ScriptsToRun.Single().Id);
        }
    }
}
