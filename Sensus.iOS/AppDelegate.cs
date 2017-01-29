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
using Xamarin;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xam.Plugin.MapExtend.iOSUnified;
using Sensus.UI;
using Sensus.Probes;
using Sensus.Context;
using Sensus.Exceptions;
using Sensus.iOS.Context;
using UIKit;
using Foundation;
using CoreLocation;
using Facebook.CoreKit;
using Sensus.iOS.Exceptions;
using Syncfusion.SfChart.XForms.iOS.Renderers;
using Sensus.iOS.Callbacks.UILocalNotifications;
using Sensus.iOS.Callbacks;
using UserNotifications;
using Sensus.iOS.Callbacks.UNUserNotifications;
using Sensus.iOS.Concurrent;
using Sensus.Encryption;

namespace Sensus.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        private CLLocationManager _locationManager = new CLLocationManager();

        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            // insights should be initialized first to maximize coverage of exception reporting
            InsightsInitialization.Initialize(new iOSInsightsInitializer(UIDevice.CurrentDevice.IdentifierForVendor.AsString()), SensusServiceHelper.XAMARIN_INSIGHTS_APP_KEY);

            #region configure context
            SensusContext.Current = new iOSSensusContext
            {
                Platform = Sensus.Context.Platform.iOS,
                MainThreadSynchronizer = new MainConcurrent(),
                Encryption = new SimpleEncryption(SensusServiceHelper.ENCRYPTION_KEY)
            };

            // iOS introduced a new notification center in 10.0 based on UNUserNotifications
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                UNUserNotificationCenter.Current.RequestAuthorizationAsync(UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Alert);
                UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
                UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
                UNUserNotificationCenter.Current.Delegate = new UNUserNotificationDelegate();
                SensusContext.Current.CallbackScheduler = new UNUserNotificationCallbackScheduler();
                SensusContext.Current.Notifier = new UNUserNotificationNotifier();
            }
            // use the pre-10.0 approach based on UILocalNotifications
            else
            {
                UIApplication.SharedApplication.RegisterUserNotificationSettings(UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Badge | UIUserNotificationType.Sound | UIUserNotificationType.Alert, new NSSet()));
                SensusContext.Current.CallbackScheduler = new UILocalNotificationCallbackScheduler();
                SensusContext.Current.Notifier = new UILocalNotificationNotifier();
            }
            #endregion

            SensusServiceHelper.Initialize(() => new iOSSensusServiceHelper());

            // facebook settings
            Settings.AppID = "873948892650954";
            Settings.DisplayName = "Sensus";

            Forms.Init();
            FormsMaps.Init();
            MapExtendRenderer.Init();
            new SfChartRenderer();
            ZXing.Net.Mobile.Forms.iOS.Platform.Init();

            LoadApplication(new App());

#if UNIT_TESTING
            Forms.ViewInitialized += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.View.StyleId))
                    e.NativeView.AccessibilityIdentifier = e.View.StyleId;
            };

            Calabash.Start();
#endif

            // background authorization will be done implicitly when the location manager is used in probes, but the authorization is
            // done asynchronously so it's likely that the probes will believe that GPS is not enabled/authorized even though the user
            // is about to grant access (if they choose). now, the health test should fix this up by checking for GPS and restarting
            // the probes, but the whole thing will seem strange to the user. instead, prompt the user for background authorization
            // immediately. this is only done one time after the app is installed and started.
            _locationManager.RequestAlwaysAuthorization();

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
            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            serviceHelper.StartAsync(async () =>
            {
                await (SensusContext.Current.CallbackScheduler as IiOSCallbackScheduler).UpdateCallbacksAsync();

#if UNIT_TESTING
                    // load and run the unit testing protocol
                    string filePath = NSBundle.MainBundle.PathForResource("UnitTestingProtocol", "json");
                    using (Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        Protocol.RunUnitTestingProtocol(file);
                    }
#endif
            });

            base.OnActivated(uiApplication);
        }

        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            // UILocalNotifications were obsoleted in iOS 10.0, and we should not be receiving them via this app delegate
            // method. we won't have any idea how to service them on iOS 10.0 and above. report the problem to Insights and bail.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
                SensusException.Report("Received UILocalNotification in iOS 10 or later.");
            else
            {
                // we're in iOS < 10.0, which means we should have a notifier and scheduler to handle the notification.

                // cancel notification (removing it from the tray), since it has served its purpose
                (SensusContext.Current.Notifier as IUILocalNotificationNotifier)?.CancelNotification(notification);

                IiOSCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as IiOSCallbackScheduler;
                if (callbackScheduler == null)
                    SensusException.Report("Invalid callback scheduler.");
                else
                {
                    // run asynchronously to release the UI thread
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        // the following must be done on the UI thread because we reference members of the UILocalNotification.
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                        {
                            callbackScheduler.ServiceCallbackAsync(notification.UserInfo);

                            // check whether the user opened the notification to open sensus, indicated by an application state that is not active. we'll
                            // also get notifications when the app is active, since we use them for timed callback events. if the user opened the notification, 
                            // display the page associated with the notification (if there is one). 
                            if (application.ApplicationState != UIApplicationState.Active && notification.UserInfo != null)
                            {
                                callbackScheduler.OpenDisplayPage(notification.UserInfo);

                                // provide some generic feedback if the user responded to a silent notification
                                if ((notification.UserInfo.ValueForKey(new NSString(iOSNotifier.SILENT_NOTIFICATION_KEY)) as NSNumber)?.BoolValue ?? false)
                                {
                                    SensusServiceHelper.Get().FlashNotificationAsync("Study Updated.", false);
                                }
                            }
                        });
                    });
                }
            }
        }

        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground(UIApplication uiApplication)
        {
            (SensusContext.Current.Notifier as IiOSNotifier).CancelSilentNotifications();

            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            serviceHelper.IssuePendingSurveysNotificationAsync(null, true);

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
            {
                if (protocol.Running)
                {
                    foreach (Probe probe in protocol.Probes)
                    {
                        if (probe.Running)
                        {
                            lock (probe.StartStopTimes)
                            {
                                probe.StartStopTimes.Add(new Tuple<bool, DateTime>(false, DateTime.Now));
                            }
                        }
                    }
                }
            }

            serviceHelper.Save();
            serviceHelper.StopProtocols();
        }
    }
}