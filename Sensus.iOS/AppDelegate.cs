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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xamarin;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xam.Plugin.MapExtend.iOSUnified;
using Sensus.Shared;
using Sensus.Shared.UI;
using Sensus.Shared.Probes;
using Sensus.Shared.Context;
using Sensus.Shared.iOS.Context;
using UIKit;
using Foundation;
using CoreLocation;
using Plugin.Toasts;
using Facebook.CoreKit;
using Syncfusion.SfChart.XForms.iOS.Renderers;

namespace Sensus.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            SensusContext.Current = new iOSSensusContext(SensusServiceHelper.ENCRYPTION_KEY);
            SensusServiceHelper.Initialize(() => new iOSSensusServiceHelper());

            // facebook settings
            Settings.AppID = "873948892650954";
            Settings.DisplayName = "Sensus";

            Forms.Init();
            FormsMaps.Init();
            MapExtendRenderer.Init();
            new SfChartRenderer();

            // toasts for iOS
            DependencyService.Register<ToastNotificatorImplementation>();
            ToastNotificatorImplementation.Init();

            LoadApplication(new App());

            uiApplication.RegisterUserNotificationSettings(UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert, new NSSet()));

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
            if (url != null)
            {
                if (url.PathExtension == "json")
                {
                    if (url.Scheme == "sensus")
                    {
                        try
                        {
                            Protocol.DeserializeAsync(new Uri("http://" + url.AbsoluteString.Substring(url.AbsoluteString.IndexOf('/') + 2).Trim()), Protocol.DisplayAndStartAsync);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to display Sensus Protocol from HTTP URL \"" + url.AbsoluteString + "\":  " + ex.Message, LoggingLevel.Verbose, GetType());
                        }
                    }
                    else if (url.Scheme == "sensuss")
                    {
                        try
                        {
                            Protocol.DeserializeAsync(new Uri("https://" + url.AbsoluteString.Substring(url.AbsoluteString.IndexOf('/') + 2).Trim()), Protocol.DisplayAndStartAsync);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to display Sensus Protocol from HTTPS URL \"" + url.AbsoluteString + "\":  " + ex.Message, LoggingLevel.Verbose, GetType());
                        }
                    }
                    else
                    {
                        try
                        {
                            Protocol.DeserializeAsync(File.ReadAllBytes(url.Path), Protocol.DisplayAndStartAsync);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to display Sensus Protocol from file URL \"" + url.AbsoluteString + "\":  " + ex.Message, LoggingLevel.Verbose, GetType());
                        }
                    }
                }
            }

            // We need to handle URLs by passing them to their own OpenUrl in order to make the Facebook SSO authentication works.
            return ApplicationDelegate.SharedInstance.OpenUrl(application, url, sourceApplication, annotation);
        }

        public override void OnActivated(UIApplication uiApplication)
        {
            // since all notifications are about to be rescheduled/serviced, clear all current notifications.
            UIApplication.SharedApplication.CancelAllLocalNotifications();
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;

            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            serviceHelper.ActivationId = Guid.NewGuid().ToString();

            try
            {
                serviceHelper.BarcodeScanner = new ZXing.Mobile.MobileBarcodeScanner(UIApplication.SharedApplication.KeyWindow.RootViewController);
            }
            catch (Exception ex)
            {
                serviceHelper.Logger.Log("Failed to create barcode scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            serviceHelper.StartAsync(() =>
            {
                serviceHelper.UpdateCallbackNotificationActivationIdsAsync();

#if UNIT_TESTING
                    // load and run the unit testing protocol
                    string filePath = NSBundle.MainBundle.PathForResource("UnitTestingProtocol", "json");
                    using (Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        Protocol.RunUnitTestingProtocol(file);
                    }
#endif

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
            if (notification.UserInfo != null)
            {
                iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

                // check whether this is a callback notification and service it if it is
                NSNumber isCallbackValue = notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.SENSUS_CALLBACK_KEY)) as NSNumber;
                if (isCallbackValue?.BoolValue ?? false)
                    serviceHelper.ServiceCallbackNotificationAsync(notification);

                // check whether the user opened a pending-survey notification (indicated by an application state that is not active). we'll
                // also get notifications when the app is active, due to how we manage pending-survey notifications.
                if (application.ApplicationState != UIApplicationState.Active)
                {
                    NSString notificationId = notification.UserInfo.ValueForKey(new NSString(SensusServiceHelper.NOTIFICATION_ID_KEY)) as NSString;
                    if (notificationId != null && notificationId.ToString() == SensusServiceHelper.PENDING_SURVEY_NOTIFICATION_ID)
                    {
                        // display the pending scripts page if it is not already on the top of the navigation stack
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                        {
                            IReadOnlyList<Page> navigationStack = Xamarin.Forms.Application.Current.MainPage.Navigation.NavigationStack;
                            Page topPage = navigationStack.Count == 0 ? null : navigationStack.Last();
                            if (!(topPage is PendingScriptsPage))
                                await Xamarin.Forms.Application.Current.MainPage.Navigation.PushAsync(new PendingScriptsPage());
                        });
                    }
                }
            }
        }

        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground(UIApplication uiApplication)
        {
            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            // app is no longer active, so reset the activation ID
            serviceHelper.ActivationId = null;

            serviceHelper.IssuePendingSurveysNotificationAsync(true, true);

            // save app state in background
            nint saveTaskId = uiApplication.BeginBackgroundTask(() =>
            {
            });

            serviceHelper.SaveAsync(() =>
            {
                uiApplication.EndBackgroundTask(saveTaskId);
            });
        }

        // This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground(UIApplication uiApplication)
        {
        }

        // This method is called when the application is about to terminate. Save data, if needed.
        public override void WillTerminate(UIApplication uiApplication)
        {
            // this method won't be called when the user kills the app using multitasking; however,
            // it should be called if the system kills the app when it's running in the background.
            // it should also be called if the system shuts down due to loss of battery power.
            // there doesn't appear to be a way to gracefully stop the app when the user kills it
            // via multitasking...we'll have to live with that. also some online resources indicate 
            // that no background time can be requested from within this method. so, instead of 
            // beginning a background task, just wait for the calls to finish.

            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            // we're going to save the service helper and its protocols/probes in the running state
            // so that they will be restarted if/when the user restarts the app. in order to properly 
            // track running time for listening probes, we need to add a stop time manually since
            // we won't call stop until after the service helper has been saved.
            foreach (Protocol protocol in serviceHelper.RegisteredProtocols)
                if (protocol.Running)
                    foreach (Probe probe in protocol.Probes)
                        if (probe.Running)
                        {
                            lock (probe.StartStopTimes)
                            {
                                probe.StartStopTimes.Add(new Tuple<bool, DateTime>(false, DateTime.Now));
                            }
                        }

            serviceHelper.Save();
            serviceHelper.StopProtocols();
        }
    }
}