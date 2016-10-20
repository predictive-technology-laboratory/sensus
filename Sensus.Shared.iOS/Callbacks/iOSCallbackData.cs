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
using UIKit;
using Foundation;
using UserNotifications;
using Sensus.Shared.Callbacks;

namespace Sensus.Shared.iOS.Callbacks
{
    public class iOSCallbackData : ICallbackData
    {
        #region Fields
        private readonly NSDictionary _data;
        #endregion

        #region Constructors
        public iOSCallbackData()
        {
            _data = new NSDictionary();
        }

        public iOSCallbackData(UNNotificationResponse data) : this(data?.Notification?.Request?.Content?.UserInfo)
        { }

        public iOSCallbackData(UILocalNotification local)
        {
            _data = local.UserInfo = new NSDictionary();
        }

        public iOSCallbackData(UNMutableNotificationContent content)
        {
            _data = content.UserInfo = new NSDictionary();
        }

        public iOSCallbackData(NSDictionary data)
        {
            _data = data;
        }
        #endregion

        #region Properties
        public NotificationType Type
        {
            get { return (NotificationType) Enum.Parse(typeof(NotificationType), (_data.ValueForKey(new NSString("NotificationType")) as NSString)?.ToString()); }
            set { _data.SetValueForKey(new NSString(value.ToString()), new NSString("NotificationType")); }
        }

        public string CallbackId
        {
            get { return (_data.ValueForKey(new NSString("CallbackId")) as NSString)?.ToString(); }
            set { _data.SetValueForKey(new NSString(value), new NSString("CallbackId")); }
        }

        public bool IsRepeating
        {
            get { return (_data.ValueForKey(new NSString("IsRepeating")) as NSNumber)?.BoolValue ?? false; }
            set { _data.SetValueForKey(new NSNumber(value), new NSString("IsRepeating")); }
        }

        public TimeSpan RepeatDelay
        {
            get { return new TimeSpan((_data.ValueForKey(new NSString("RepeatDelay")) as NSNumber)?.LongValue ?? 0); }
            set { _data.SetValueForKey(new NSNumber(value.Ticks), new NSString("RepeatDelay")); }
        }

        public bool LagAllowed
        {
            get { return (_data.ValueForKey(new NSString("LagAllowed")) as NSNumber)?.BoolValue ?? false; }
            set { _data.SetValueForKey(new NSNumber(value), new NSString("LagAllowed")); }
        }

        public string ActivationId
        {
            get { return (_data.ValueForKey(new NSString("ActivationId")) as NSString)?.ToString(); }
            set { _data.SetValueForKey(new NSString(value), new NSString("ActivationId")); }
        }
        #endregion
    }
}
