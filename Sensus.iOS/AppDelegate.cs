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
using Facebook.CoreKit;
using Syncfusion.SfChart.XForms.iOS.Renderers;
using Sensus.iOS.Callbacks.UILocalNotifications;
using Sensus.iOS.Callbacks;
using UserNotifications;
using Sensus.iOS.Callbacks.UNUserNotifications;
using Sensus.iOS.Concurrent;
using Sensus.Encryption;
using Microsoft.AppCenter.Crashes;

namespace Sensus.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            #region configure context
            SensusContext.Current = new iOSSensusContext
            {
                Platform = Sensus.Context.Platform.iOS,
                MainThreadSynchronizer = new MainConcurrent(),
                SymmetricEncryption = new SymmetricEncryption(SensusServiceHelper.ENCRYPTION_KEY)
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

#if UI_TESTING
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
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    // ensure service helper is running
                    await SensusServiceHelper.Get().StartAsync();

                    // update/run all callbacks
                    await (SensusContext.Current.CallbackScheduler as IiOSCallbackScheduler).UpdateCallbacksAsync();

#if UI_TESTING
                    // load and run the UI testing protocol
                    string filePath = NSBundle.MainBundle.PathForResource("UiTestingProtocol", "json");
                    using (Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        Protocol.RunUiTestingProtocol(file);
                    }
#endif
                }
                catch(Exception ex)
                {
                    SensusException.Report("Failed in OnActivated.", ex);
                }
            });

            base.OnActivated(uiApplication);
        }

        /// <summary>
        /// Pre-10.0:  Handles notifications received when the app is in the foreground. See <see cref="UNUserNotificationDelegate.WillPresentNotification"/> for
        /// the corresponding handler for iOS 10.0 and above.
        /// </summary>
        /// <param name="application">Application.</param>
        /// <param name="notification">Notification.</param>
        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            // UILocalNotifications were obsoleted in iOS 10.0, and we should not be receiving them via this app delegate
            // method. we won't have any idea how to service them on iOS 10.0 and above. report the problem and bail.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                SensusException.Report("Received UILocalNotification in iOS 10 or later.");
            }
            else
            {
                // we're in iOS < 10.0, which means we should have a notifier and scheduler to handle the notification.

                // cancel notification (removing it from the tray), since it has served its purpose
                (SensusContext.Current.Notifier as IUILocalNotificationNotifier)?.CancelNotification(notification);

                iOSCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as iOSCallbackScheduler;

                if (callbackScheduler == null)
                {
                    SensusException.Report("We don't have an iOSCallbackScheduler.");
                }
                else if(notification.UserInfo == null)
                {
                    SensusException.Report("Null user info passed to ReceivedLocalNotification.");
                }
                else
                {
                    // run asynchronously to release the UI thread
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        // service the callback if we've got one (not all notification userinfo bundles are for callbacks)
                        if (callbackScheduler.IsCallback(notification.UserInfo))
                        {
                            await callbackScheduler.ServiceCallbackAsync(notification.UserInfo);

                            // provide some generic feedback if the user responded to a silent callback notification
                            if (callbackScheduler.TryGetCallback(notification.UserInfo)?.Silent ?? false)
                            {
                                SensusServiceHelper.Get().FlashNotificationAsync("Study Updated.");
                            }
                        }

                        // open the the display page if we're passed one. the following must be done on the UI 
                        // thread because we reference UIApplicationState.Active.
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                        {
                            // check whether the user opened the notification to open sensus, indicated by an application state that is not active. we'll
                            // also get notifications when the app is active, since we use them for timed callback events. if the user opened the notification, 
                            // display the page associated with the notification (if there is one). 
                            if (application.ApplicationState != UIApplicationState.Active)
                            {
                                callbackScheduler.OpenDisplayPage(notification.UserInfo);
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
            (SensusContext.Current.CallbackScheduler as iOSCallbackScheduler).CancelSilentNotifications();

            iOSSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as iOSSensusServiceHelper;

            serviceHelper.IssuePendingSurveysNotificationAsync(null, true);

            // save app state in background
            nint saveTaskId = uiApplication.BeginBackgroundTask(() =>
            {
                // not much we can do if we run out of time...
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