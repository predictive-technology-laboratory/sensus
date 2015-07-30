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
using Toasts.Forms.Plugin.iOS;
using System.IO;
using Facebook.CoreKit;
using Xamarin;
using Xam.Plugin.MapExtend.iOSUnified;

namespace Sensus.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        private iOSSensusServiceHelper _serviceHelper;

        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            SensusServiceHelper.Initialize(() => new iOSSensusServiceHelper());

            // facebook settings
            Settings.AppID = "873948892650954";
            Settings.DisplayName = "Sensus";

            Forms.Init();
            FormsMaps.Init();
            MapExtendRenderer.Init();

            ToastNotificatorImplementation.Init();

            App app = new App();
            LoadApplication(app);

            uiApplication.RegisterUserNotificationSettings(UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert, new NSSet()));

            _serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            UiBoundSensusServiceHelper.Set(_serviceHelper);
            app.SensusMainPage.DisplayServiceHelper(UiBoundSensusServiceHelper.Get(true));

            if (launchOptions != null)
            {                
                NSObject launchOptionValue;
                if (launchOptions.TryGetValue(UIApplication.LaunchOptionsLocalNotificationKey, out launchOptionValue))
                    ServiceNotificationAsync(launchOptionValue as UILocalNotification);
                else if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUrlKey, out launchOptionValue))
                    Protocol.DisplayFromBytesAsync(File.ReadAllBytes((launchOptionValue as NSUrl).Path));
            }

            // service all other notifications whose fire time has passed
            foreach (UILocalNotification notification in uiApplication.ScheduledLocalNotifications)
                if (notification.FireDate.ToDateTime() <= DateTime.UtcNow)
                    ServiceNotificationAsync(notification);

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            if (url.AbsoluteString.EndsWith(".sensus"))
                try
                {
                    Protocol.DisplayFromBytesAsync(File.ReadAllBytes(url.Path));
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to display Sensus Protocol from URL \"" + url.AbsoluteString + "\":  " + ex.Message, LoggingLevel.Verbose, GetType());
                }

            // We need to handle URLs by passing them to their own OpenUrl in order to make the Facebook SSO authentication works.
            return ApplicationDelegate.SharedInstance.OpenUrl(application, url, sourceApplication, annotation);
        }

        public override void OnActivated(UIApplication uiApplication)
        {
            // since all notifications are about to be rescheduled, clear any scheduled / delivered notifications.
            UIApplication.SharedApplication.CancelAllLocalNotifications();
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;

            _serviceHelper.ActivationId = Guid.NewGuid().ToString();

            iOSSensusServiceHelper sensusServiceHelper = UiBoundSensusServiceHelper.Get(true) as iOSSensusServiceHelper;

            sensusServiceHelper.StartAsync(() =>
                {
                    sensusServiceHelper.UpdateCallbackNotificationActivationIdsAsync();
                });
            
            base.OnActivated(uiApplication);
        }

        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            ServiceNotificationAsync(notification);
        }

        private void ServiceNotificationAsync(UILocalNotification notification)
        {
            bool isCallback = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_KEY)) as NSNumber).BoolValue;
            if (isCallback)
            {   
                // cancel notification, since it has served its purpose
                iOSSensusServiceHelper.CancelLocalNotification(notification);

                string callbackId = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_ID_KEY)) as NSString).ToString();
                bool repeating = (notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
                int repeatDelayMS = (notification.UserInfo.ValueForKey(new NSString(iOSSensusServiceHelper.SENSUS_CALLBACK_REPEAT_DELAY)) as NSNumber).Int32Value;
                string activationId = (notification.UserInfo.ValueForKey(new NSString(iOSSensusServiceHelper.SENSUS_CALLBACK_ACTIVATION_ID)) as NSString).ToString();

                // only raise callback if it's from the current activation and if it is scheduled
                if (activationId != _serviceHelper.ActivationId || !iOSSensusServiceHelper.Get().CallbackIsScheduled(callbackId))
                    return;                      

                nint taskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
                    {
                        // if we're out of time running in the background, cancel the callback.
                        UiBoundSensusServiceHelper.Get(true).CancelRaisedCallback(callbackId);
                    });

                UiBoundSensusServiceHelper.Get(true).RaiseCallbackAsync(callbackId, repeating, false, () =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                            {
                                // notification has been serviced, so end background task
                                UIApplication.SharedApplication.EndBackgroundTask(taskId);

                                // update and schedule notification again if it was a repeating callback
                                if (repeating)
                                {
                                    notification.FireDate = DateTime.UtcNow.AddMilliseconds((double)repeatDelayMS).ToNSDate();
                                    UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                                }
                                else
                                    _serviceHelper.UnscheduleOneTimeCallback(callbackId);
                            });
                    });
            }
        }
		
        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground(UIApplication application)
        {
            iOSSensusServiceHelper serviceHelper = UiBoundSensusServiceHelper.Get(false) as iOSSensusServiceHelper;
            if (serviceHelper != null)
            {
                serviceHelper.SaveAsync();

                // app is no longer active, so reset the activation ID
                serviceHelper.ActivationId = null;
            }
        }
		
        // This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground(UIApplication application)
        {
        }
		
        // This method is called when the application is about to terminate. Save data, if needed.
        public override void WillTerminate(UIApplication application)
        {
            _serviceHelper.Dispose();
        }                
    }
}