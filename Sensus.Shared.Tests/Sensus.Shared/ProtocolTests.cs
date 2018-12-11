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
using Newtonsoft.Json;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sensus.Tests.Classes;
using Sensus.Context;

namespace Sensus.Tests
{
    
    public class ProtocolTests
    {
        #region Fields
        private JsonSerializerSettings _jsonSerializerSettings;
        #endregion

        #region SetUp
        public ProtocolTests()
        {
            _jsonSerializerSettings = SensusServiceHelper.JSON_SERIALIZER_SETTINGS;

            // we don't want to quietly handle errors when testing.
            _jsonSerializerSettings.Error = null;

            SensusServiceHelper.ClearSingleton();
        }
        #endregion

        [Fact]
        public async Task ProtocolSerializeDeserializeTest()
        {
            SensusServiceHelper.Initialize(() => new TestSensusServiceHelper());

            var protocol1 = await Protocol.CreateAsync("abc");
            protocol1.ContactEmail = "ContactEmail";
            protocol1.ContinueIndefinitely = true;
            protocol1.Description = "Description";
            protocol1.EndDate = DateTime.MaxValue;
            protocol1.EndTime = TimeSpan.MaxValue;
            protocol1.GpsDesiredAccuracyMeters = 0.1f;
            protocol1.GpsMinDistanceDelayMeters = 0.2f;
            protocol1.GpsMinTimeDelayMS = 10;
            protocol1.Groupable = true;
            protocol1.VariableValueUiProperty = new List<string>(new string[] { "var1: val1", "var1:", "var1:val2", "var2", "var2:" });

            Protocol protocol2 = null;
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                protocol2 = JsonConvert.DeserializeObject<Protocol>(JsonConvert.SerializeObject(protocol1, _jsonSerializerSettings), _jsonSerializerSettings);
            });

            Assert.Equal(protocol1.Name, protocol2.Name);
            Assert.Equal(protocol1.ContactEmail, protocol2.ContactEmail);
            Assert.Equal(protocol1.ContinueIndefinitely, protocol2.ContinueIndefinitely);
            Assert.Equal(protocol1.Description, protocol2.Description);
            Assert.Equal(protocol1.EndDate, protocol2.EndDate);
            Assert.Equal(protocol1.EndTime, protocol2.EndTime);
            Assert.Equal(protocol1.GpsDesiredAccuracyMeters, protocol2.GpsDesiredAccuracyMeters);
            Assert.Equal(protocol1.GpsMinDistanceDelayMeters, protocol2.GpsMinDistanceDelayMeters);
            Assert.Equal(protocol1.GpsMinTimeDelayMS, protocol2.GpsMinTimeDelayMS);
            Assert.Equal(protocol1.Groupable, protocol2.Groupable);
            Assert.Equal(protocol2.VariableValue.Count, 2);
            Assert.Equal(protocol2.VariableValue["var1"], "val2");
            Assert.Equal(protocol2.VariableValue["var2"], null);            
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
        //        Assert.Equal(protocol1.Name, protocol2.Name);
        //        Assert.Equal(protocol1.ContactEmail, protocol2.ContactEmail);
        //        Assert.Equal(protocol1.ContinueIndefinitely, protocol2.ContinueIndefinitely);
        //        Assert.Equal(protocol1.Description, protocol2.Description);
        //        Assert.Equal(protocol1.EndDate, protocol2.EndDate);
        //        Assert.Equal(protocol1.EndTime, protocol2.EndTime);
        //        Assert.Equal(protocol1.ForceProtocolReportsToRemoteDataStore, protocol2.ForceProtocolReportsToRemoteDataStore);
        //        Assert.Equal(protocol1.GpsDesiredAccuracyMeters, protocol2.GpsDesiredAccuracyMeters);
        //        Assert.Equal(protocol1.GpsMinDistanceDelayMeters, protocol2.GpsMinDistanceDelayMeters);
        //        Assert.Equal(protocol1.GpsMinTimeDelayMS, protocol2.GpsMinTimeDelayMS);
        //        Assert.Equal(protocol1.Groupable, protocol2.Groupable);
        //    });
        //}
    }
}
