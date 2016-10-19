using System;
using Foundation;
using Sensus.Shared.Notifications;
using UIKit;
using UserNotifications;

namespace Sensus.Shared.iOS.Notifications
{
    public class iOSNotifyMeta : INotifyMeta
    {
        private readonly NSDictionary _data;

        public iOSNotifyMeta()
        {
            _data = new NSDictionary();
        }

        public iOSNotifyMeta(UNNotificationResponse data) : this(data?.Notification?.Request?.Content?.UserInfo)
        { }

        public iOSNotifyMeta(UILocalNotification local)
        {
            _data = local.UserInfo = new NSDictionary();
        }

        public iOSNotifyMeta(UNMutableNotificationContent content)
        {
            _data = content.UserInfo = new NSDictionary();
        }

        public iOSNotifyMeta(NSDictionary data)
        {
            _data = data;
        }

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
    }
}
