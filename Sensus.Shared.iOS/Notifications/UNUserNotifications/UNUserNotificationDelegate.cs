﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using Sensus.Shared.Context;
using UserNotifications;

namespace Sensus.Shared.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            base.DidReceiveNotificationResponse(center, response, completionHandler);

            (SensusContext.Current.Notifier as IUNUserNotificationNotifier).CancelNotification(response?.Notification.Request.Identifier);

            (SensusContext.Current.CallbackScheduler as IiOSCallbackScheduler)?.ServiceCallbackAsync(response?.Notification?.Request?.Content?.UserInfo);
        }
    }
}
