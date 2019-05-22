using System;
using System.Collections.Generic;
using System.Text;
using Android.Views.Accessibility;
using Android.AccessibilityServices;
using Sensus.Android;
using Android.App;
using Android;
using Android.Util;
using Sensus.Probes.Apps;
using Android.Content;
using Sensus.Exceptions;

namespace Sensus.Android.Probes.Apps
{
    [Service(Label = "AndroidKeystrokeService", Permission = Manifest.Permission.BindAccessibilityService)]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    public class AndroidKeystrokeService : AccessibilityService
    {
        private AccessibilityServiceInfo info = new AccessibilityServiceInfo();
        public static event EventHandler<KeystrokeDatum> AccessibilityBroadcast;
        public override void OnAccessibilityEvent(AccessibilityEvent e)
        {
            try { 
                if (e.Text.Count > 0)
                {
                    //string key = e.Text.ToString();
                    //Console.WriteLine("***** OnAccessibilityEvent ***** " +e.Text.Count +"   " +e.EventType.ToString() + "   " + e.PackageName + "  " + e.Text[0]);

                    AccessibilityBroadcast?.Invoke(this, new KeystrokeDatum(DateTimeOffset.UtcNow, e.Text[0].ToString(), e.PackageName));
                }
                
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception in Kesystroke service:  " + ex.Message, ex);
            }
    //StoreDatumAsync(new KeystrokeDatum(DateTimeOffset.UtcNow, heading));
}

        public override void OnCreate()
        {
            base.OnCreate();
            
        }

        public override void OnInterrupt()
        {
            throw new NotImplementedException();
        }

        public override ComponentName StartService(Intent service)
        {
            SensusServiceHelper.Get().FlashNotificationAsync("start");

            //info.EventTypes = EventTypes.ViewTextChanged;
            //info.FeedbackType = FeedbackFlags.AllMask;
            //info.NotificationTimeout = 100;
            //this.SetServiceInfo(info);

            return base.StartService(service);
        }

        public override bool StopService(Intent name)
        {
            SensusServiceHelper.Get().FlashNotificationAsync("stop");
            return base.StopService(name);
        }

        protected override void OnServiceConnected()
        {
            base.OnServiceConnected();
            info.EventTypes = EventTypes.ViewTextChanged;
            info.FeedbackType = FeedbackFlags.AllMask;
            info.NotificationTimeout = 100;
            this.SetServiceInfo(info);

        }
    }
}
