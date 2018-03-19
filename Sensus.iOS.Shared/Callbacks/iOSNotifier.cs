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
using Sensus.Callbacks;

namespace Sensus.iOS.Callbacks
{
    public abstract class iOSNotifier : Notifier, IiOSNotifier
    {
        /// <summary>
        /// The notification identifier key, which has a value uniquely identifying the issued notification.
        /// </summary>
        public const string NOTIFICATION_ID_KEY = "SENSUS-NOTIFICATION-ID";

        /// <summary>
        /// The silent notification key, which has a value indicating that the notification is silent. See <see cref="IiOSNotifier"/> for more
        /// on silent notifications.
        /// </summary>
        public const string SILENT_NOTIFICATION_KEY = "SENSUS-SILENT-NOTIFICATION";

        /// <summary>
        /// Checks whether a notification information dictionary represents a silent notification.
        /// </summary>
        /// <returns><c>true</c>, if silent, <c>false</c> otherwise.</returns>
        /// <param name="notificationInfo">Notification info.</param>
        public static bool IsSilent(NSDictionary notificationInfo)
        {
            return (notificationInfo?.ValueForKey(new NSString(SILENT_NOTIFICATION_KEY)) as NSNumber)?.BoolValue ?? false;
        }

        /// <summary>
        /// Cancels the silent notifications.
        /// </summary>
        public abstract void CancelSilentNotifications();
    }
}