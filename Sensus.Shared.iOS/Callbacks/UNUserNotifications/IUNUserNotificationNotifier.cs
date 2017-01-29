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
using Foundation;
using Sensus.Callbacks;
using UserNotifications;

namespace Sensus.iOS.Callbacks.UNUserNotifications
{
    /// <summary>
    /// Adds notifier methods specific to the UNNotificationCenter architecture.
    /// </summary>
    public interface IUNUserNotificationNotifier : IiOSNotifier
    {
        void IssueSilentNotificationAsync(string id, TimeSpan delay, NSMutableDictionary info, Action<UNNotificationRequest> requestCreated = null);

        void IssueNotificationAsync(string title, string message, string id, string protocolId, bool playSound, DisplayPage displayPage, TimeSpan delay, NSMutableDictionary info, Action<UNNotificationRequest> requestCreated = null);

        void IssueNotificationAsync(string id, UNNotificationContent content, TimeSpan delay, Action<UNNotificationRequest> requestCreated = null);

        void IssueNotificationAsync(UNNotificationRequest request, Action<NSError> errorCallback = null);

        void CancelNotification(UNNotificationRequest request);
    }
}