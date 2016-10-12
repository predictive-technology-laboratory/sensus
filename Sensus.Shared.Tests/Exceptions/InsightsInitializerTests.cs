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
using System;

namespace Sensus.Shared.Tests.Exceptions
{
    [TestFixture]
    public class IInsightsInitializerTests
    {
        #region Classes
        private class ActionInitializer : IInsightsInitializer
        {
            private readonly Action<string> _action;

            public ActionInitializer(Action<string> action)
            {
                _action = action;
            } 

            public void Initialize(string insightsKey)
            {
                _action(insightsKey);
            }
        }
        #endregion        

        #region Fields
        private readonly IInsightsInitializer _platformInitializer;
        #endregion

        #region Constructors
        public IInsightsInitializerTests(): this(new ActionInitializer(s => {})) { }

        protected IInsightsInitializerTests(IInsightsInitializer platformInitializer)
        {
            _platformInitializer = platformInitializer;
        }
        #endregion

        [Test]
        public void PlatformInitializerDoesNotExplodeWithKeyTest()
        {
            Assert.DoesNotThrow(() => InsightInitialization.Initialize(_platformInitializer, "Fake-Key", false));
        }

        [Test]
        public void PlatformInitializerDoesNotExplodeWithoutKeyTest()
        {
            Assert.DoesNotThrow(() => InsightInitialization.Initialize(_platformInitializer, "", false));
        }

        [Test]
        public void InitializerCalledTest()
        {
            Assert.Throws<SuccessException>(() => InsightInitialization.Initialize(new ActionInitializer(s => { throw new SuccessException("You passed the test"); }), "Fake-Key", false));
        }

        [Test]
        public void InitializerGivenCorrectKeyAsParameterTest()
        {
            InsightInitialization.Initialize(new ActionInitializer(s => { Assert.AreEqual("Fake-Key", s); }), "Fake-Key", false);
        }

        [Test]
        public void InitializerNotCalledWhenKeyIsEmptyTest()
        {
            InsightInitialization.Initialize(new ActionInitializer(s => { Assert.Fail(); }), "", false);
        }

        [Test]
        public void InitializerExceptionsSuprressedTest()
        {
            Assert.DoesNotThrow(() => InsightInitialization.Initialize(new ActionInitializer(s => { throw new Exception(); }), "Fake-Key"));
        }
    }
}
