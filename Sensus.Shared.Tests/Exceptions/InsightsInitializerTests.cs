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

using NUnit.Framework;
using Sensus.Shared.Exceptions;

namespace Sensus.Shared.Tests.Exceptions
{
    [TestFixture]
    public class IInsightsInitializerTests
    {
        #region Classes
        private class DummyInitializer : IInsightsInitializer
        {
            public void Initialize(string insightsKey)
            {
                
            }
        }

        private class SuccessInitializer: IInsightsInitializer
        {
            public void Initialize(string insightsKey)
            {
                throw new SuccessException("You passed the test");
            }
        }
        #endregion

        #region Fields
        private readonly IInsightsInitializer _initializer;
        #endregion

        #region Constructors
        public IInsightsInitializerTests(): this(new DummyInitializer()) { }

        protected IInsightsInitializerTests(IInsightsInitializer initializer)
        {
            _initializer = initializer;
        }
        #endregion

        [Test]
        public void PlatformInitializerDoesNotExplodeWithKeyTest()
        {
            Assert.DoesNotThrow(() => InsightInitialization.Initialize(_initializer, "Fake-Key", false));
        }

        [Test]
        public void PassedInInitializerCalledTest()
        {
            Assert.Throws<SuccessException>(() => InsightInitialization.Initialize(new SuccessInitializer(), "Fake-Key", false));
        }

        [Test]
        public void InitializerNotCalledWhenKeyIsEmptyTest()
        {
            Assert.DoesNotThrow(() => InsightInitialization.Initialize(new SuccessInitializer(), "", false));
        }

        [Test]
        public void AllExceptionsSuprressedTest()
        {
            Assert.DoesNotThrow(() => InsightInitialization.Initialize(new SuccessInitializer(), "Fake-Key"));
        }
    }
}
