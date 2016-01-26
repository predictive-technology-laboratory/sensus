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
using Plugin.Toasts;
using System.IO;
using Facebook.CoreKit;
using Xamarin;
using Xam.Plugin.MapExtend.iOSUnified;
using CoreLocation;
using System.Threading;

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

            // toasts for iOS
            DependencyService.Register<ToastNotificatorImplementation>();
            ToastNotificatorImplementation.Init();

            LoadApplication(new App());

            uiApplication.RegisterUserNotificationSettings(UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert, new NSSet()));

            _serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            #if UNIT_TESTING
            Forms.ViewInitialized += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.View.StyleId))
                    e.NativeView.AccessibilityIdentifier = e.View.StyleId;
            };

            Calabash.Start();
            #endif

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            if (url.AbsoluteString.EndsWith(".json"))
            {
                try
                {
                    Protocol.DeserializeAsync(File.ReadAllBytes(url.Path), true, Protocol.DisplayAndStartAsync);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to display Sensus Protocol from URL \"" + url.AbsoluteString + "\":  " + ex.Message, LoggingLevel.Verbose, GetType());
                }
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

            try
            {
                _serviceHelper.BarcodeScanner = new ZXing.Mobile.MobileBarcodeScanner(UIApplication.SharedApplication.KeyWindow.RootViewController);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Failed to create barcode scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            iOSSensusServiceHelper sensusServiceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            sensusServiceHelper.StartAsync(() =>
                {
                    sensusServiceHelper.UpdateCallbackNotificationActivationIdsAsync();

                    #if UNIT_TESTING
                    // load and run the unit testing protocol
                    string filePath = NSBundle.MainBundle.PathForResource("UnitTestingProtocol", "json");
                    using (Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        Protocol.RunUnitTestingProtocol(file);
                    }
                    #endif

                    Device.BeginInvokeOnMainThread(() =>
                        {
                            (App.Current as App).ProtocolsPage.Bind();
                        });
                });

            // background authorization will be done implicitly when the location manager is used in probes, but the authorization is
            // done asynchronously so it's likely that the probes will believe that GPS is not enabled/authorized even though the user
            // is about to grant access (if they choose). now, the health test should fix this up by checking for GPS and restarting
            // the probes, but the whole thing will seem strange to the user. instead, prompt the user for background authorization
            // immediately. this is only done one time after the app is installed and started.
            new CLLocationManager().RequestAlwaysAuthorization();

            base.OnActivated(uiApplication);
        }

        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            ServiceNotificationAsync(notification);
        }

        private void ServiceNotificationAsync(UILocalNotification notification)
        {
            if (notification.UserInfo != null)
            {
                NSNumber isCallbackValue = notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_KEY)) as NSNumber;
                if (isCallbackValue != null && isCallbackValue.BoolValue)
                    _serviceHelper.ServiceCallbackNotificationAsync(notification);
            }
        }
		
        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground(UIApplication application)
        {
            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;
            if (serviceHelper != null)
            {
                // save app state in background
                nint saveTaskId = application.BeginBackgroundTask(() =>
                    {
                    });

                serviceHelper.SaveAsync(() =>
                    {
                        application.EndBackgroundTask(saveTaskId);
                    });              

                // app is no longer active, so reset the activation ID
                serviceHelper.ActivationId = null;

                if (iOSSensusServiceHelper.PromptForInputsRunning)
                    serviceHelper.IssueNotificationAsync("Please tap to provide responses.", null);
            }
        }
		
        // This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground(UIApplication application)
        {
        }
		
        // This method is called when the application is about to terminate. Save data, if needed.
        public override void WillTerminate(UIApplication application)
        {
            // this method won't be called when the user kills the app using multitasking; however,
            // it should be called if the system kills the app when it's running in the background.
            // there doesn't appear to be a way to gracefully stop the app when the user kills it
            // via multitasking...we'll have to live with that. also some online resources indicate 
            // that no background time can be requested from within this method. so, instead of 
            // beginning a background task, just wait for the calls to finish.
            if (_serviceHelper != null)
            {
                _serviceHelper.Save();
                _serviceHelper.Stop(false);
            }
        }
    }
}