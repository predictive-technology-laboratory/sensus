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

using Sensus.Shared.Context;
using Sensus.Shared.Concurrent;
using Sensus.Shared.Encryption;
using Sensus.Shared.iOS.Concurrent;
using UIKit;
using Sensus.Shared.iOS.Callbacks.UNUserNotifications;
using Sensus.Shared.iOS.Callbacks.UILocalNotifications;
using Sensus.Shared.Callbacks;

namespace Sensus.Shared.iOS.Context
{
    public class iOSSensusContext : ISensusContext
    {
        public Platform Platform { get; }
        public IConcurrent MainThreadSynchronizer { get; }
        public IEncryption Encryption { get; }
        public ICallbackScheduler CallbackScheduler { get; }
        public INotifier Notifier { get; }
        public string ActivationId { get; set; }

        #region Constructor
        public iOSSensusContext(string encryptionKey)
        {
            Platform = Platform.iOS;
            MainThreadSynchronizer = new MainConcurrent();
            Encryption = new SimpleEncryption(encryptionKey);

            // iOS introduced a new notification center in 10.0 based on UNUserNotifications
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                CallbackScheduler = new iOSUNUserNotificationCallbackScheduler();
                Notifier = new iOSUNUserNotificationNotifier();
            }
            // use the pre-10.0 approach based on UILocalNotifications
            else
            {
                CallbackScheduler = new iOSUILocalNotificationCallbackScheduler();
                Notifier = new iOSUILocalNotificationNotifier();
            }
        }
        #endregion
    }
}
