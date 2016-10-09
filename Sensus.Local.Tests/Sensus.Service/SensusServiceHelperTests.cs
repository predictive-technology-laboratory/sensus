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
using NUnit.Framework;
using Newtonsoft.Json;
using Sensus.Service.iOS.Context;
using Sensus.Service.Tools.Context;
using Sensus.Tools;
using SensusService;
using SensusService.Probes.Location;
using SensusService.Probes.User.Scripts;

namespace Sensus.Local.Tests
{
    [TestFixture]
    public class SensusServiceHelperTests
    {
        #region Fields
        private JsonSerializerSettings _jsonSerializerSettings;
        #endregion

        #region SetUp
        [TestFixtureSetUp]
        public void SetUp()
        {
            _jsonSerializerSettings = SensusServiceHelper.JSON_SERIALIZER_SETTINGS;

            //we don't want to quietly handle errors when testing.
            _jsonSerializerSettings.Error = null;

            SensusContext.Current = new TestSensusContext();

            SensusServiceHelper.ClearSingleton();
        }
        #endregion

        [Test]
        public void SerializeAndDeserializeNoExceptionTest()
        {
            var service = new TestSensusServiceHelper();

            var serial   = JsonConvert.SerializeObject(service, _jsonSerializerSettings);
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.AreEqual(0, service.RegisteredProtocols.Count);
            Assert.AreEqual(0, service2.RegisteredProtocols.Count);
        }

        [Test]
        public void RegisteredOneProtocolTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

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
            SensusServiceHelper.Initialize(() => service1);

            service1.RegisteredProtocols.Clear();

            Protocol.Create("Test1");
            Protocol.Create("Test2");

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);
            SensusServiceHelper.ClearSingleton();
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.AreEqual(2, service1.RegisteredProtocols.Count);
            Assert.AreEqual(2, service2.RegisteredProtocols.Count);

            Assert.AreEqual(service1.RegisteredProtocols.First().Name, service2.RegisteredProtocols.First().Name);

            Assert.AreEqual(service1.RegisteredProtocols.Skip(0).Take(1).Single().Name, service2.RegisteredProtocols.Skip(0).Take(1).Single().Name);
            Assert.AreEqual(service1.RegisteredProtocols.Skip(1).Take(1).Single().Name, service2.RegisteredProtocols.Skip(1).Take(1).Single().Name);
        }

        [Test]
        public void RunningProtocolIdsTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service);

            service.RunningProtocolIds.Clear();

            Protocol.Create("Test");

            foreach (var protocol in service.RegisteredProtocols)
            {
                service.RunningProtocolIds.Add(protocol.Id);
            }

            var serial = JsonConvert.SerializeObject(service, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.AreEqual(service.RunningProtocolIds.Count, service2.RunningProtocolIds.Count);
            Assert.AreEqual(service.RunningProtocolIds.First(), service2.RunningProtocolIds.First());
        }

        [Test]
        public void PointsOfInterestTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service);

            service.PointsOfInterest.Clear();

            service.PointsOfInterest.Add(new PointOfInterest("Test", "Test", null));

            var serial = JsonConvert.SerializeObject(service);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.PointsOfInterest.Count, service2.PointsOfInterest.Count);
            Assert.AreEqual(service.PointsOfInterest.First().Name, service2.PointsOfInterest.First().Name);
        }

        [Test]
        public void FlashNotificationsEnabledTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service);

            service.FlashNotificationsEnabled = true;

            var serial = JsonConvert.SerializeObject(service);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);

            service.FlashNotificationsEnabled = false;

            serial = JsonConvert.SerializeObject(service);

            SensusServiceHelper.ClearSingleton();

            service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);
        }

        [Test]
        public void ScriptsToRunTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service);

            service.RunningProtocolIds.Clear();

            service.ScriptsToRun.Add(new Script(new ScriptRunner("Test", new ScriptProbe())));

            var serial = JsonConvert.SerializeObject(service);

            SensusServiceHelper.ClearSingleton();
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.ScriptsToRun.Count, service2.ScriptsToRun.Count);
            Assert.AreEqual(service.ScriptsToRun.First().Id, service2.ScriptsToRun.First().Id);
        }
    }
}
