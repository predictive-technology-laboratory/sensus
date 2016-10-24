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

using Sensus.Shared.Encryption;
using Sensus.Shared.Concurrent;
using Sensus.Shared.Android.Concurrent;
using Sensus.Shared.Context;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Android.Callbacks;
using Xamarin.Forms.Platform.Android;

namespace Sensus.Shared.Android.Context
{
    public class AndroidSensusContext<MainActivityT> : ISensusContext where MainActivityT : FormsApplicationActivity
    {
        public Sensus.Shared.Context.Platform Platform { get; }
        public IConcurrent MainThreadSynchronizer { get; }
        public IEncryption Encryption { get; }
        public ICallbackScheduler CallbackScheduler { get; }
        public INotifier Notifier { get; }
        public string ActivationId { get; set; }

        public AndroidSensusContext(string encryptionKey, AndroidSensusService<MainActivityT> service)
        {
            Platform = Sensus.Shared.Context.Platform.Android;
            MainThreadSynchronizer = new MainConcurrent();
            Encryption = new SimpleEncryption(encryptionKey);
            CallbackScheduler = new AndroidCallbackScheduler<MainActivityT>(service);
            Notifier = new AndroidNotifier<MainActivityT>(service);
        }
    }
}