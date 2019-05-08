using System;
using System.Collections.Generic;
using System.Text;
using Android.Views.Accessibility;
using Android.AccessibilityServices;
using Sensus.Android;
using Android.App;
using Android;

namespace Sensus.Android.Probes.Apps
{
    [Service(Label = "AndroidKeystrokeService", Permission = Manifest.Permission.BindAccessibilityService)]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    public class AndroidKeystrokeService : AccessibilityService
    {
        private AccessibilityServiceInfo info = new AccessibilityServiceInfo();
        public override void OnAccessibilityEvent(AccessibilityEvent e)
        {

            Console.WriteLine("***** OnAccessibilityEvent ***** "+ e.EventType.ToString());
        }

        public override void OnInterrupt()
        {
            throw new NotImplementedException();
        }

        protected override void OnServiceConnected()
        {
            base.OnServiceConnected();
            info.EventTypes = EventTypes.ViewTextChanged;
            info.FeedbackType =FeedbackFlags.AllMask;
            info.NotificationTimeout = 100;
            this.SetServiceInfo(info);

        }
    }
}
