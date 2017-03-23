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

using Sensus.Context;
using Sensus.Concurrent;
using Sensus.Encryption;
using Sensus.Callbacks;

namespace Sensus.iOS.Context
{
    public class iOSSensusContext : ISensusContext
    {
        public Platform Platform { get; set; }
        public IConcurrent MainThreadSynchronizer { get; set; }
        public IEncryption Encryption { get; set; }
        public ICallbackScheduler CallbackScheduler { get; set; }
        public INotifier Notifier { get; set; }
        public string ActivationId { get; set; }
    }
}