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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sensus.DataStores;
using Xunit;

namespace Sensus.Tests.DataStores
{
    public class DataStoreTests
    {
        /// <summary>
        /// Ensure that we can construct all data stores using the constructorless parameter and without
        /// the presence of the service helper. This is necessary because, upon deserialization of
        /// the service helper, the service helper singleton will be null and unavailable for 
        /// referencing from within the data store constructors.
        [Fact]
        public void ParameterlessConstructorTest()
        {
            SensusServiceHelper.ClearSingleton();
            Assert.True(Assembly.GetExecutingAssembly()
                                .GetTypes()
                                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(DataStore)))
                                .Select(Activator.CreateInstance)
                                .ToList().Count > 0);
        }
    }
}
