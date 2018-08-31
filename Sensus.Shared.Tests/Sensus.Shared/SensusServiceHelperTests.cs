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
using Xunit;
using Sensus.Tests.Classes;
using Sensus.Probes.Location;
using Sensus.Probes.User.Scripts;
using System;
using System.Threading.Tasks;

namespace Sensus.Tests
{    
    public class SensusServiceHelperTests
    {
        private JsonSerializerSettings _jsonSerializerSettings;

        public SensusServiceHelperTests()
        {
            _jsonSerializerSettings = SensusServiceHelper.JSON_SERIALIZER_SETTINGS;

            //we don't want to quietly handle errors when testing.
            _jsonSerializerSettings.Error = null;

            SensusServiceHelper.ClearSingleton();
        }

        [Fact]
        public void SerializeAndDeserializeNoExceptionTest()
        {
            var service1 = new TestSensusServiceHelper();

            var serial1 = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);
            service1.RegisteredProtocols.Clear();
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial1, _jsonSerializerSettings);

            Assert.Equal(0, service1.RegisteredProtocols.Count);
            Assert.Equal(0, service2.RegisteredProtocols.Count);
        }

        [Fact]
        public async Task RegisteredOneProtocolTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            service1.RegisteredProtocols.Clear();

            Protocol protocol = await Protocol.CreateAsync("asdf");

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.Equal(service1.RegisteredProtocols.Count, service2.RegisteredProtocols.Count);
            Assert.Equal(service1.RegisteredProtocols.First().Name, service2.RegisteredProtocols.First().Name);
        }

        [Fact]
        public async Task RegisteredTwoProtocolsTest()
        {
            TestSensusServiceHelper service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            service1.RegisteredProtocols.Clear();

            await Protocol.CreateAsync("Test1");
            await Protocol.CreateAsync("Test2");

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);
            SensusServiceHelper.ClearSingleton();
            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.Equal(2, service1.RegisteredProtocols.Count);
            Assert.Equal(2, service2.RegisteredProtocols.Count);

            Assert.Equal(service1.RegisteredProtocols.Skip(0).Take(1).Single().Name, service2.RegisteredProtocols.Skip(0).Take(1).Single().Name);
            Assert.Equal(service1.RegisteredProtocols.Skip(1).Take(1).Single().Name, service2.RegisteredProtocols.Skip(1).Take(1).Single().Name);
        }

        [Fact]
        public async Task RunningProtocolIdsTest()
        {
            var service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            await Protocol.CreateAsync("Test");

            service1.RunningProtocolIds.Clear();
            service1.RunningProtocolIds.Add(service1.RegisteredProtocols.Single().Id);

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.Equal(service1.RunningProtocolIds.Count, service2.RunningProtocolIds.Count);
            Assert.Equal(service1.RunningProtocolIds.Single(), service2.RunningProtocolIds.Single());
        }

        [Fact]
        public void PointsOfInterestTest()
        {
            var service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            service1.PointsOfInterest.Clear();
            service1.PointsOfInterest.Add(new PointOfInterest("Test", "Test", null));

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial, _jsonSerializerSettings);

            Assert.Equal(service1.PointsOfInterest.Count, service2.PointsOfInterest.Count);
            Assert.Equal(service1.PointsOfInterest.Single().Name, service2.PointsOfInterest.Single().Name);
        }

        [Fact]
        public void FlashNotificationsEnabledTest()
        {
            var service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            service1.FlashNotificationsEnabled = true;

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.Equal(service1.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);

            service1.FlashNotificationsEnabled = false;

            serial = JsonConvert.SerializeObject(service1);

            SensusServiceHelper.ClearSingleton();

            service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.Equal(service1.FlashNotificationsEnabled, service2.FlashNotificationsEnabled);
        }

        [Fact]
        public void ScriptsToRunTest()
        {
            var service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            service1.RunningProtocolIds.Clear();

            service1.ScriptsToRun.Add(new Script(new ScriptRunner("Test", new ScriptProbe())));

            var serial = JsonConvert.SerializeObject(service1, _jsonSerializerSettings);

            SensusServiceHelper.ClearSingleton();

            var service2 = JsonConvert.DeserializeObject<TestSensusServiceHelper>(serial);

            Assert.Equal(service1.ScriptsToRun.Count, service2.ScriptsToRun.Count);
            Assert.Equal(service1.ScriptsToRun.Single().Id, service2.ScriptsToRun.Single().Id);
        }

        [Fact]
        public async Task ScriptsDisplayDateTimeOrderTest()
        {
            var service1 = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => service1);

            ScriptRunner runner = new ScriptRunner("Test", new ScriptProbe());
            Random random = new Random();
            for (int i = 0; i < 50; ++i)
            {
                Script script = new Script(runner);
                script.ScheduledRunTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(random.Next(-100000, 100000));
                await service1.AddScriptAsync(script, RunMode.Multiple);
            }

            Script scriptMin = new Script(runner);
            scriptMin.ScheduledRunTime = DateTimeOffset.MinValue;
            await service1.AddScriptAsync(scriptMin, RunMode.Multiple);

            Script scriptMax = new Script(runner);
            scriptMax.ScheduledRunTime = DateTimeOffset.MaxValue;
            await service1.AddScriptAsync(scriptMax, RunMode.Multiple);

            for (int i = 1; i < service1.ScriptsToRun.Count; ++i)
            {
                Assert.True(service1.ScriptsToRun.ElementAt(i).DisplayDateTime >= service1.ScriptsToRun.ElementAt(i - 1).DisplayDateTime);
            }
        }
    }
}
