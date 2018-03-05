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
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;

namespace Sensus.Tests.Core
{
    [TestFixture]
    public class ProtocolTests
    {
        #region Fields
        private JsonSerializerSettings _jsonSerializerSettings;
        #endregion

        #region SetUp
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            _jsonSerializerSettings = SensusServiceHelper.JSON_SERIALIZER_SETTINGS;

            //we don't want to quietly handle errors when testing.
            _jsonSerializerSettings.Error = null;
        }
        #endregion

        [Test]
        public void ProtocolSerializeDeserializeTest()
        {
            var protocol1 = new Protocol("abc")
            {
                ContactEmail = "ContactEmail",
                ContinueIndefinitely = true,
                Description = "Description",
                EndDate = DateTime.MaxValue,
                EndTime = TimeSpan.MaxValue,
                GpsDesiredAccuracyMeters = 0.1f,
                GpsMinDistanceDelayMeters = 0.2f,
                GpsMinTimeDelayMS = 10,
                Groupable = true,
                VariableValueUiProperty = new List<string>(new string[] { "var1: val1", "var1:", "var1:val2", "var2", "var2:" })
            };

            var protocol2 = JsonConvert.DeserializeObject<Protocol>(JsonConvert.SerializeObject(protocol1, _jsonSerializerSettings), _jsonSerializerSettings);

            Assert.AreEqual(protocol1.Name, protocol2.Name);
            Assert.AreEqual(protocol1.ContactEmail, protocol2.ContactEmail);
            Assert.AreEqual(protocol1.ContinueIndefinitely, protocol2.ContinueIndefinitely);
            Assert.AreEqual(protocol1.Description, protocol2.Description);
            Assert.AreEqual(protocol1.EndDate, protocol2.EndDate);
            Assert.AreEqual(protocol1.EndTime, protocol2.EndTime);
            Assert.AreEqual(protocol1.GpsDesiredAccuracyMeters, protocol2.GpsDesiredAccuracyMeters);
            Assert.AreEqual(protocol1.GpsMinDistanceDelayMeters, protocol2.GpsMinDistanceDelayMeters);
            Assert.AreEqual(protocol1.GpsMinTimeDelayMS, protocol2.GpsMinTimeDelayMS);
            Assert.AreEqual(protocol1.Groupable, protocol2.Groupable);
            Assert.AreEqual(protocol2.VariableValue.Count, 2);
            Assert.AreEqual(protocol2.VariableValue["var1"], "val2");
            Assert.AreEqual(protocol2.VariableValue["var2"], null);            
        }

        //[Test, Explicit ("Too many dependencies to get this working right now")]
        //public void ProtocolSerializeEncryptDeserializeTest()
        //{
        //    var protocol1 = new Protocol("abc")
        //    {
        //        ContactEmail                          = "ContactEmail",
        //        ContinueIndefinitely                  = true,
        //        Description                           = "Description",
        //        EndDate                               = DateTime.MaxValue,
        //        EndTime                               = TimeSpan.MaxValue,
        //        ForceProtocolReportsToRemoteDataStore = true,
        //        GpsDesiredAccuracyMeters              = 0.1f,
        //        GpsMinDistanceDelayMeters             = 0.2f,
        //        GpsMinTimeDelayMS                     = 10,
        //        Groupable                             = true
        //    };

        //    var serialize1 = Context.SensusContext.Current.Encryption.Encrypt(JsonConvert.SerializeObject(protocol1, _jsonSerializerSettings));            

        //    Protocol.DeserializeAsync(serialize1, protocol2 =>
        //    {
        //        Assert.AreEqual(protocol1.Name, protocol2.Name);
        //        Assert.AreEqual(protocol1.ContactEmail, protocol2.ContactEmail);
        //        Assert.AreEqual(protocol1.ContinueIndefinitely, protocol2.ContinueIndefinitely);
        //        Assert.AreEqual(protocol1.Description, protocol2.Description);
        //        Assert.AreEqual(protocol1.EndDate, protocol2.EndDate);
        //        Assert.AreEqual(protocol1.EndTime, protocol2.EndTime);
        //        Assert.AreEqual(protocol1.ForceProtocolReportsToRemoteDataStore, protocol2.ForceProtocolReportsToRemoteDataStore);
        //        Assert.AreEqual(protocol1.GpsDesiredAccuracyMeters, protocol2.GpsDesiredAccuracyMeters);
        //        Assert.AreEqual(protocol1.GpsMinDistanceDelayMeters, protocol2.GpsMinDistanceDelayMeters);
        //        Assert.AreEqual(protocol1.GpsMinTimeDelayMS, protocol2.GpsMinTimeDelayMS);
        //        Assert.AreEqual(protocol1.Groupable, protocol2.Groupable);
        //    });
        //}
    }
}