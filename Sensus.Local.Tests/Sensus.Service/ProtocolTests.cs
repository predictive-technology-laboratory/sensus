using System;
using System.Threading;
using Newtonsoft.Json;
using SensusService;
using NUnit.Framework;
using Sensus.Service.iOS.Context;
using Sensus.Service.Tools.Context;

namespace Sensus.Local.Tests
{
    [TestFixture]
    public class ProtocolTests
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

            SensusContext.Current = new TestSensusContext();
        }
        #endregion

        [Test]
        public void ProtocolSerializeDeserializeTest()
        {
            var protocol1 = new Protocol("abc")
            {
                ContactEmail                          = "ContactEmail",
                ContinueIndefinitely                  = true,
                Description                           = "Description",
                EndDate                               = DateTime.MaxValue,
                EndTime                               = TimeSpan.MaxValue,
                ForceProtocolReportsToRemoteDataStore = true,
                GpsDesiredAccuracyMeters              = 0.1f,
                GpsMinDistanceDelayMeters             = 0.2f,
                GpsMinTimeDelayMS                     = 10,
                Groupable                             = true
            };

            
            var protocol2 = JsonConvert.DeserializeObject<Protocol>(JsonConvert.SerializeObject(protocol1, _jsonSerializerSettings), _jsonSerializerSettings);

            Assert.AreEqual(protocol1.Name, protocol2.Name);
            Assert.AreEqual(protocol1.ContactEmail, protocol2.ContactEmail);
            Assert.AreEqual(protocol1.ContinueIndefinitely, protocol2.ContinueIndefinitely);
            Assert.AreEqual(protocol1.Description, protocol2.Description);
            Assert.AreEqual(protocol1.EndDate, protocol2.EndDate);
            Assert.AreEqual(protocol1.EndTime, protocol2.EndTime);
            Assert.AreEqual(protocol1.ForceProtocolReportsToRemoteDataStore, protocol2.ForceProtocolReportsToRemoteDataStore);
            Assert.AreEqual(protocol1.GpsDesiredAccuracyMeters, protocol2.GpsDesiredAccuracyMeters);
            Assert.AreEqual(protocol1.GpsMinDistanceDelayMeters, protocol2.GpsMinDistanceDelayMeters);
            Assert.AreEqual(protocol1.GpsMinTimeDelayMS, protocol2.GpsMinTimeDelayMS);
            Assert.AreEqual(protocol1.Groupable, protocol2.Groupable);
        }

        [Test]
        public void ProtocolSerializeEncryptDeserializeTest()
        {
            var protocol1 = new Protocol("abc")
            {
                ContactEmail                          = "ContactEmail",
                ContinueIndefinitely                  = true,
                Description                           = "Description",
                EndDate                               = DateTime.MaxValue,
                EndTime                               = TimeSpan.MaxValue,
                ForceProtocolReportsToRemoteDataStore = true,
                GpsDesiredAccuracyMeters              = 0.1f,
                GpsMinDistanceDelayMeters             = 0.2f,
                GpsMinTimeDelayMS                     = 10,
                Groupable                             = true
            };

            var serialize1 = SensusServiceHelper.Encrypt(JsonConvert.SerializeObject(protocol1, _jsonSerializerSettings));
            var runWait = new ManualResetEvent(false);

            Protocol.DeserializeAsync(serialize1, protocol2 =>
            {
                Assert.AreEqual(protocol1.Name, protocol2.Name);
                Assert.AreEqual(protocol1.ContactEmail, protocol2.ContactEmail);
                Assert.AreEqual(protocol1.ContinueIndefinitely, protocol2.ContinueIndefinitely);
                Assert.AreEqual(protocol1.Description, protocol2.Description);
                Assert.AreEqual(protocol1.EndDate, protocol2.EndDate);
                Assert.AreEqual(protocol1.EndTime, protocol2.EndTime);
                Assert.AreEqual(protocol1.ForceProtocolReportsToRemoteDataStore, protocol2.ForceProtocolReportsToRemoteDataStore);
                Assert.AreEqual(protocol1.GpsDesiredAccuracyMeters, protocol2.GpsDesiredAccuracyMeters);
                Assert.AreEqual(protocol1.GpsMinDistanceDelayMeters, protocol2.GpsMinDistanceDelayMeters);
                Assert.AreEqual(protocol1.GpsMinTimeDelayMS, protocol2.GpsMinTimeDelayMS);
                Assert.AreEqual(protocol1.Groupable, protocol2.Groupable);
            });
        }
    }
}