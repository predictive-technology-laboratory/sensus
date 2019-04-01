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

using Foundation;
using Sensus.Notifications;

namespace Sensus.iOS.Notifications
{
    public abstract class iOSNotifier : Notifier
    {
        /// <summary>
        /// Key for the notification identifier, which is a value uniquely identifying the issued notification.
        /// </summary>
        public const string NOTIFICATION_ID_KEY = "SENSUS-NOTIFICATION-ID";

        public abstract void RemoveAllNotifications();
    }
}