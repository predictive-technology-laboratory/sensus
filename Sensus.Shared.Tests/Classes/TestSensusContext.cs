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
using Sensus.Callbacks;
using Sensus.Concurrent;
using Sensus.Context;
using Sensus.Encryption;

namespace Sensus.Tests.Classes
{
    public class TestSensusContext : ISensusContext
    {
        public Platform Platform { get; set; }
        public IConcurrent MainThreadSynchronizer { get; set; }
        public IEncryption SymmetricEncryption { get; set; }
        public INotifier Notifier { get; set; }
        public ICallbackScheduler CallbackScheduler { get; set; }
        public string ActivationId { get; set; }
        public string IamAccessKey { get; set; }
        public string IamAccessKeySecret { get; set; }

        public TestSensusContext()
        {
            Platform = Platform.Test;
            MainThreadSynchronizer = new LockConcurrent();
            SymmetricEncryption = new SymmetricEncryption("91091462A8D6FD3B4DB1D91C731070F10460D73AEE0377EDC2585C42F70A84A5");
            Notifier = new TestSensusNotifier();
            CallbackScheduler = new TestSensusCallbackScheduler();
            ActivationId = "asdfadsf";
        }
    }
}