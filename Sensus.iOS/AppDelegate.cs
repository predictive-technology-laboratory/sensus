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
using System.Linq;
using System.Collections.Generic;

using Foundation;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using SensusUI;
using SensusService;
using Xamarin.Geolocation;

namespace Sensus.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        private UIWindow _window;    
        private iOSSensusServiceHelper _sensusServiceHelper;

        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            Forms.Init();

            _window = new UIWindow(UIScreen.MainScreen.Bounds);

            App app = new App();
            LoadApplication(app);

            uiApplication.RegisterUserNotificationSettings(UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert, new NSSet()));

            _sensusServiceHelper = SensusServiceHelper.Load<iOSSensusServiceHelper>() as iOSSensusServiceHelper;

            UiBoundSensusServiceHelper.Set(_sensusServiceHelper);
            app.SensusMainPage.DisplayServiceHelper(UiBoundSensusServiceHelper.Get(true));

            // if this app was started by a local notification, service that notification
            NSObject launchingNotification;
            if (launchOptions != null && launchOptions.TryGetValue(UIApplication.LaunchOptionsLocalNotificationKey, out launchingNotification))
                ServiceNotification(launchingNotification as UILocalNotification);

            // service all other notifications whose fire time has passed
            foreach (UILocalNotification notification in uiApplication.ScheduledLocalNotifications)
                if (notification.FireDate.ToDateTime() <= DateTime.UtcNow)
                    ServiceNotification(notification);

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        public override void OnActivated(UIApplication uiApplication)
        {
            iOSSensusServiceHelper sensusServiceHelper = UiBoundSensusServiceHelper.Get(true) as iOSSensusServiceHelper;
            sensusServiceHelper.StartAsync(null);
            sensusServiceHelper.RefreshCallbackNotifications();
            base.OnActivated(uiApplication);
        }

        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            ServiceNotification(notification);
        }

        private void ServiceNotification(UILocalNotification notification)
        {
            bool isCallback = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_KEY)) as NSNumber).BoolValue;
            if (isCallback)
            {              
                int callbackId = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_ID_KEY)) as NSNumber).Int32Value;
                bool repeating = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
                int repeatDelayMS = (notification.UserInfo.ValueForKey(new NSString(iOSSensusServiceHelper.SENSUS_CALLBACK_REPEAT_DELAY)) as NSNumber).Int32Value;

                nint taskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
                    {
                        // if we're out of time running in the background, cancel the callback.
                        UiBoundSensusServiceHelper.Get(true).CancelRaisedCallback(callbackId);
                    });

                UiBoundSensusServiceHelper.Get(true).RaiseCallbackAsync(callbackId, repeating, () =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                            {
                                UIApplication.SharedApplication.EndBackgroundTask(taskId);
                                UIApplication.SharedApplication.CancelLocalNotification(notification);  

                                if (repeating)
                                {
                                    notification.FireDate = DateTime.UtcNow.AddMilliseconds((double)repeatDelayMS).ToNSDate();
                                    UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                                }
                            });
                    });
            }
        }
		
        // This method is invoked when the application is about to move from active to inactive state.
        // OpenGL applications should use this method to pause.
        public override void OnResignActivation(UIApplication application)
        {
        }
		
        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground(UIApplication application)
        {
            // TODO:  Stop probes that are not allowed to run in background
        }
		
        // This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground(UIApplication application)
        {
            // TODO:  Restart all probes that weren't allowed to run in the background
        }
		
        // This method is called when the application is about to terminate. Save data, if needed.
        public override void WillTerminate(UIApplication application)
        {
            _sensusServiceHelper.Destroy();
        }
    }
}

