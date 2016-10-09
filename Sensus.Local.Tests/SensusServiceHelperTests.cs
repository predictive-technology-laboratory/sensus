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
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Sensus.Tools;
using SensusService.Probes.Movement;
using SensusService.Probes.User.Scripts;

namespace Sensus.Local.Tests
{
    [TestFixture]
    public class SensusServiceHelperTests
    {
        [Test]
        public void SerializeTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper(new LockConcurrent());
            SensusService.SensusServiceHelper.Initialize(() => service);

            var serial = JsonConvert.SerializeObject(service);
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service, service2);
        }

        [Test]
        public void RegisteredProtocolsTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper(new LockConcurrent());
            SensusService.SensusServiceHelper.Initialize(() => service);

            service.RegisteredProtocols.Clear();

            SensusService.Protocol.Create("Test");

            var serial = JsonConvert.SerializeObject(service);
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.RegisteredProtocols.Count, service2.RegisteredProtocols.Count);
            Assert.AreEqual(service.RegisteredProtocols.First().Name, service2.RegisteredProtocols.First().Name);
        }

        [Test]
        public void RunningProtocolIdsTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper(new LockConcurrent());
            SensusService.SensusServiceHelper.Initialize(() => service);

            service.RunningProtocolIds.Clear();

            SensusService.Protocol.Create("Test");

            foreach (var protocol in service.RegisteredProtocols)
            {
                service.RunningProtocolIds.Add(protocol.Id);
            }

            var serial = JsonConvert.SerializeObject(service);
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.RunningProtocolIds.Count, service2.RunningProtocolIds.Count);
            Assert.AreEqual(service.RunningProtocolIds.First(), service2.RunningProtocolIds.First());
        }

        [Test]
        public void PointsOfInterestTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper(new LockConcurrent());
            SensusService.SensusServiceHelper.Initialize(() => service);

            service.PointsOfInterest.Clear();

            service.PointsOfInterest.Add(new SensusService.Probes.Location.PointOfInterest("Test", "Test", null));

            var serial = JsonConvert.SerializeObject(service);
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.PointsOfInterest.Count, service2.PointsOfInterest.Count);
            Assert.AreEqual(service.PointsOfInterest.First().Name, service2.PointsOfInterest.First().Name);
        }

        [Test]
        public void FlashNotificationsEnabledTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper(new LockConcurrent());
            SensusService.SensusServiceHelper.Initialize(() => service);

            service.FlashNotificationsEnabled = true;

            var serial = JsonConvert.SerializeObject(service);
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);

            service.FlashNotificationsEnabled = false;

            serial = JsonConvert.SerializeObject(service);
            service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);
        }

        [Test]
        public void ScriptsToRunTest()
        {
            TestSensusServiceHelper service = new TestSensusServiceHelper(new LockConcurrent());
            SensusService.SensusServiceHelper.Initialize(() => service);

            service.RunningProtocolIds.Clear();

            service.ScriptsToRun.Add(new Script(new ScriptRunner("Test", new ScriptProbe())));

            var serial = JsonConvert.SerializeObject(service);
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.AreEqual(service.ScriptsToRun.Count, service2.ScriptsToRun.Count);
            Assert.AreEqual(service.ScriptsToRun.First().Id, service2.ScriptsToRun.First().Id);
        }
    }
}
