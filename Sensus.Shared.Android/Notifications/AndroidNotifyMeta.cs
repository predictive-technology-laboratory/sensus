using System;
using Android.Content;
using Sensus.Shared.Notifications;

namespace Sensus.Shared.Android.Notifications
{
    public class AndroidNotifyMeta: INotifyMeta
    {
        #region Fields
        private readonly Intent _intent;
        #endregion

        #region Constructors
        public AndroidNotifyMeta(Intent intent)
        {
            _intent = intent;
        }
        #endregion

        #region Properties
        public NotificationType Type
        {
            get { return (NotificationType)Enum.Parse(typeof(NotificationType), _intent.GetStringExtra("NotficationType")); }
            set { _intent.PutExtra("NotficationType", value.ToString()); }
        }

        public string CallbackId
        {
            get { return _intent.GetStringExtra("CallbackId"); }
            set { _intent.PutExtra("CallbackId", value); }
        }

        public bool IsRepeating
        {
            get { return _intent.GetBooleanExtra("IsRepeating", false); }
            set { _intent.PutExtra("IsRepeating", value); }
        }

        public TimeSpan RepeatDelay
        {
            get { return new TimeSpan(_intent.GetLongExtra("RepeatDelay", 0)); }
            set { _intent.PutExtra("RepeatDelay", value.Ticks); }
        }

        public bool LagAllowed
        {
            get { return _intent.GetBooleanExtra("LagAllowed", false); }
            set { _intent.PutExtra("LagAllowed", value); }
        }
        #endregion
    }
}
