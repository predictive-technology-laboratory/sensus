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

using Sensus.Tools.Tests;
using Sensus.Android.Concurrent;
using NUnit.Framework;
using Xamarin.UITest;

namespace Sensus.Tests.Android.Concurrent
{
    [TestFixture]
    public class MainConcurrentTests : IConcurrentTests
    {
        #region Fields
        private IApp _app;
        #endregion

        [SetUp]
        public void BeforeEachTest()
        {
            _app = ConfigureApp.Android.StartApp(); //.ApkFile("../../../Sensus.Android/bin/Release/edu.virginia.sie.ptl.sensus.apk").StartApp();
            Concurrent = new MainConcurrent();
        }
    }
}